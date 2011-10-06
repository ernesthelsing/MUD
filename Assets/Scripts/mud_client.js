var skin : GUISkin;
var showChat : boolean = true;

var connectToIP : String = "127.0.0.1";
var connectPort : int = 25001;

var fonteCliente : Font; 

private var inputField : String = "";
private var scrollPosition : Vector2;
private var width : int = 600;
private var height : int = 320;
private var playerName : String;
private var window : Rect;

private var chatEntries = new ArrayList();
class ChatEntry {
	var time : String = "";
	var name : String = "";
	var text : String = "";
}

class PlayerNode {
		var playerName : String = "";
		var networkPlayer : NetworkPlayer;
}

private var playerList = new ArrayList();

function Awake(){
	window = Rect(Screen.width/2 - width/2, Screen.height - height+5, width, height);

	playerName = PlayerPrefs.GetString("playerName", "");

	if(!playerName || playerName == "") {
		playerName = "Jogador_" + Random.Range(1,99); 
	}

}

function OnConnectedToServer() {
	ShowChatWindow();
	networkView.RPC ("TellServerOurName", RPCMode.Server, playerName);
}


function OnDisconnectedFromServer() {
	CloseChatWindow();
}

function CloseChatWindow() {
	showChat = false;
	inputField = "";
	chatEntries = new ArrayList();
}

function ShowChatWindow() {
	showChat = true;
	inputField = "";
	chatEntries = new ArrayList();
}

function OnGUI() {
	if(!showChat) {
		return;
	}
	
	if(!fonteCliente) {
		Debug.LogError("Faltou definir a fonte no inspector!");
	}
	else {
		GUI.skin.font = fonteCliente;
	}
	
	// Código de conexão
	if(Network.peerType == NetworkPeerType.Disconnected) {
		GUILayout.Label("Conexão: Desconectado");

		connectToIP = GUILayout.TextField(connectToIP, GUILayout.MinWidth(100));
		connectPort = parseInt(GUILayout.TextField(connectPort.ToString()));
		playerName = GUILayout.TextField(playerName, GUILayout.MinWidth(100));

		GUILayout.BeginVertical();

		if (GUILayout.Button ("Conectar ao servidor"))
		{
			//Connect to the "connectToIP" and "connectPort" as entered via the GUI
			//Ignore the NAT for now
			Network.Connect(connectToIP, connectPort);
		}

		GUILayout.EndVertical();
	}
	else {
		// Cliente conectado
		if (Network.peerType == NetworkPeerType.Connecting){
		
			GUILayout.Label("Connection status: Connecting");
			
		}
		else {
			
			GUILayout.Label("Connection status: Client!");
			GUILayout.Label("Ping to server: "+Network.GetAveragePing(  Network.connections[0] ) );		
			
		}
		
		if (GUILayout.Button ("Desconectar " + playerName)) {
			
				Network.Disconnect (200);
				Application.Quit();
		}
	}

	GUI.skin = skin;

	if(Event.current.type == EventType.keyDown && Event.current.character == "\n" && inputField.Length <=0) {
		GUI.FocusWindow(5);
		GUI.FocusControl("Chat input field");
	}

	window = GUI.Window(5, window, GlobalChatWindow, "MUD Client");
}

function GlobalChatWindow(id : int) {
	
	GUILayout.BeginVertical();
	GUILayout.Space(10);
	GUILayout.EndVertical();

	scrollPosition = GUILayout.BeginScrollView(scrollPosition);

	for(var entry : ChatEntry in chatEntries) {
		
		GUILayout.BeginHorizontal();
		if(entry.name == "") {
			GUILayout.Label(entry.text);
		}
		else {
			GUILayout.Label(entry.name + ": " + entry.text);
		}

		GUILayout.EndHorizontal();
		GUILayout.Space(3);
	}

	GUILayout.EndScrollView();

	if(Event.current.type == EventType.keyDown && Event.current.character == "\n" && inputField.Length > 0) {
		HitEnter(inputField);
	}

	GUI.SetNextControlName("Chat input field");
	inputField = GUILayout.TextField(inputField);

}

/*
 * @brief	Envia a mensagem de texto digitada direto para o servidor, através de RPC
 * @param	msg	String com a mensagem a ser enviada
 * @return	void
 */
function HitEnter(msg : String) {
	msg = msg.Replace("\n","");
	networkView.RPC("ApplyGlobalChatText", RPCMode.Server, AddTimeStamp(), playerName, msg); // Manda a mensagem somente para o servidor
	inputField = ""; // Clear line
}

function addGameChatMessage(str : String) {
	//ApplyGlobalChatText("","", str);

	if(NetworkPlayer.connections.length > 0) {
		networkView.RPC("ApplyGlobalChatText", RPCMode.Others, "Vim", "Teste", str);
	}
}

@RPC
//Sent by newly connected clients, recieved by server
function TellServerOurName(name : String, info : NetworkMessageInfo){
	var newEntry : PlayerNode = new PlayerNode();
	newEntry.playerName=name;
	newEntry.networkPlayer=info.sender;
	playerList.Add(newEntry);
	
	addGameChatMessage(name+" joined the chat");
}

@RPC
function ApplyGlobalChatText (timeStamp : String, name : String, msg : String)
{
	var entry = new ChatEntry();
	entry.time = timeStamp;
	entry.name = name;
	entry.text = msg;

	chatEntries.Add(entry);
	
	//FIXME: há um erro aqui: estamos removendo as entradas do chat pelo seu número, mas uma entrada pode
	//ocupar mais de uma linha. O ideal é remover as entradas conforme o número de linhas utilizadas
	//Remove old entries
	if (chatEntries.Count > 40){
		chatEntries.RemoveAt(0);
	}

	scrollPosition.y = 1000000;	
}

/*****************************************************************************/
/*
 * Funções auxiliares: seria melhor colocá-las em um script separado?
 */

/*
 * @brief	Função que obtém a hora atual do sistema e a retorna como uma string
 * @param	void
 * @return	A hora atual do sistema em formato de string
 */
function AddTimeStamp() {

	// Nova tentativa de adicionar um time stamp à mensagem: agora colocar a hora do sistema
	return System.DateTime.Now.ToString("HH:mm:ss");
}


	



