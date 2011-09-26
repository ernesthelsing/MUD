using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MudServer : MonoBehaviour
{

	public GUISkin skin;
	public string connectToIP = "127.0.0.1";
	public int connectPort = 25001;

	public Font Fonte;

	private Vector2 scrollPosition;

	private int width = 580;
	private int height = 280;

	private string stPlayerName;

	private Rect window;
	
	private mud_regras scriptRegras;


	class PlayerNode
	{
		public string stPlayerName;
		public NetworkPlayer networkPlayer;
	};
	
	class ChatEntry {
		public string time;
		public string name;
		public string text;
	};

	private List<PlayerNode> playerList;
	private List<ChatEntry> chatEntries;


	// Use this for initialization
	void Awake ()
	{
		
		window = new Rect (Screen.width / 2 - width / 2, Screen.height - height + 5, width, height);
		
		scriptRegras = gameObject.GetComponent<mud_regras>();
		
		chatEntries = new List<ChatEntry>();
		playerList = new List<PlayerNode>();
	}

	// Update is called once per frame
	void Update ()
	{
		
	}

	/// <summary>
	/// Funcao para obter o PlayerNode à partir do networkPlayers
	/// </summary>
	/// <param name="networkPlayer">
	/// A <see cref="NetworkPlayer"/>
	/// </param>
	PlayerNode GetPlayerNode (NetworkPlayer networkPlayer)
	{
		
		foreach (PlayerNode entry in playerList) {
			
			if (entry.networkPlayer == networkPlayer) {
				return entry;
			}
		}
		
		Debug.LogError ("GetPlayerNode: Requisitou um playernode de um jogador inexistente!");
		return null;
	}

	private void OnPlayerConnected (NetworkPlayer npPlayer)
	{
		
		addGameChatMessage ("Novo jogador conectado: " + npPlayer.ipAddress + ":" + npPlayer.port);
		
	}

	private void OnPlayerDisconnected (NetworkPlayer npPlayer)
	{
		
		addGameChatMessage ("Jogador desconectado: " + npPlayer.ipAddress + ":" + npPlayer.port);
		
		// Remove o player da lista do servidor
		playerList.Remove (GetPlayerNode (npPlayer));
	}

	/// <summary>
	/// Funcão RPC para informar ao servidor da conexão de um novo cliente
	/// </summary>
	/// <param name="stName">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="info">
	/// A <see cref="NetworkMessageInfo"/>
	/// </param>
	[RPC]
	public void TellServerOurName (string stName, NetworkMessageInfo info)
	{
		string stWelcomeMsg = "Bem vindo ao servidor de MUD!\n";
		
		PlayerNode newEntry = new PlayerNode ();
		newEntry.stPlayerName = stName;
		newEntry.networkPlayer = info.sender;
		playerList.Add (newEntry);
		
		// TODO: adicionar verificacões, como login repetido, etc
		
		// Adiciona o novo jogador
		stWelcomeMsg += scriptRegras.AddNewPlayer (newEntry.networkPlayer, newEntry.stPlayerName);
		
		addGameChatMessage (newEntry.stPlayerName + " juntou-se ao servidor.");
		SendChatMessageTo (newEntry.networkPlayer, stWelcomeMsg);
		
	}

	[RPC]
	private void ApplyGlobalChatText (string stTimeStamp, string stName, string stMsg)
	{
		
		ChatEntry entry = new ChatEntry();
		entry.time = stTimeStamp;
		entry.name = stName;
		entry.text = stMsg;
		
		// DEBUG
		Debug.Log ("@ " + entry.time + " de " + entry.name + ": " + entry.text);
		
		if (entry.name != "Servidor") {
			// Não passa adiante mensagens do servidor
			// Passa a mensagem do script de regras
			scriptRegras.AddNewMessage (entry.time, entry.name, entry.text);
		}
		
		chatEntries.Add(entry);
		
		// Remove entries antigas
		if (chatEntries.Count > 20) {
			chatEntries.RemoveAt (0);
		}
		
		scrollPosition.y = 1000000;
	}

	public void OnGUI ()
	{
		
		if (!Fonte) {
			Debug.LogError ("Faltou definir a fonte no Inspector do projeto!");
		} else {
			
			GUI.skin.font = Fonte;
		}
		
		// Código de conexão
		if (Network.peerType == NetworkPeerType.Disconnected) {
			
			GUILayout.Label ("Estado da conexao: Desconectado");
			
			connectToIP = GUILayout.TextField (connectToIP, GUILayout.MinWidth (100));
			connectPort = System.Int32.Parse(GUILayout.TextField (connectPort.ToString ()));
			GUILayout.BeginVertical ();
			if (GUILayout.Button ("Iniciar servidor")) {
				
				// Inicializa o servidor para até 4 conexões
				Network.InitializeServer (4, connectPort, false);
				// Servidor inicializado com sucesso; coloca um aviso na tela
				addGameChatMessage ("Servidor inicializado.");
			}
			
			GUILayout.EndVertical ();
		} else {
			
			// Cliente conectado
			if (Network.peerType == NetworkPeerType.Connecting) {
				
				GUILayout.Label ("Status da conexao: Conectando");
			} else {
				
				GUILayout.Label ("Status da conexao: Servidor");
				GUILayout.Label ("Conexoes: " + Network.connections.Length);
				
				if (Network.connections.Length >= 1) {
					
					GUILayout.Label ("Ping para o primeiro cliente: " + Network.GetAveragePing (Network.connections[0]));
				}
			}

			if (GUILayout.Button ("Desconectar")) {
			
				Network.Disconnect (200);
			}
		}
		
		GUI.skin = skin;
		
		window = GUI.Window (5, window, GlobalChatWindow, "Mensagens do Servidor");
		
	}

	void GlobalChatWindow (int nId)
	{
		
		// Um espacamento entre os botões e a janela de mensagens...
		GUILayout.BeginVertical ();
		GUILayout.Space (10);
		GUILayout.EndVertical ();
		
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		
		foreach(ChatEntry entry in chatEntries) {
			
			GUILayout.BeginHorizontal ();
			
			if (entry.name == "") {
				
				GUILayout.Label (entry.text);
			} else {
				
				GUILayout.Label ("(" + entry.time + ")" + entry.name + ": " + entry.text);
			}
			
			GUILayout.EndHorizontal ();
			GUILayout.Space (3);
		}
		
		GUILayout.EndScrollView ();
	}

	/// <summary>
	/// Mostra uma mensagem na tela de mensagens
	/// </summary>
	public void addGameChatMessage (string stStr)
	{
		
		ApplyGlobalChatText (AddTimeStamp (), "Servidor", stStr);
	}

	/// <summary>
	/// Envia uma mensagem para um cliente específico na rede
	/// </summary>
	public void SendChatMessageTo (NetworkPlayer playerReceiver, string stMsg)
	{
		
		// DEBUG
		Debug.Log("SendChatMessageTo| " + playerReceiver);
		
		networkView.RPC("ApplyGlobalChatText", playerReceiver, AddTimeStamp (), "", stMsg);
	}

	/// <summary>
	/// Obtém a hora atual do sistema e converte em uma string para ser mostrado
	/// junto com as mensagens
	/// </summary>
	private string AddTimeStamp ()
	{
		
		return System.DateTime.Now.ToString ("HH:mm:ss");
	}
	
}







