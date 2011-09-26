using UnityEngine;
using System.Collections.Generic;

public class mud_regras : MonoBehaviour
{

	enum MudVerbs
	{
		examinar,
		mover,
		pegar,
		largar,
		inventorio,
		usar,
		falar,
		cochichar,
		ajuda
	}

	class MudCommands
	{
		public MudVerbs eVerb;
		public NetworkPlayer npSender;
	}


	/// <summary>
	/// Classe que recebe uma mensagem do servidor
	/// </summary>
	class MessageEntry
	{
		public int nId;
		public string stTime;
		public string stSender;
		public string stMsg;
		public NetworkPlayer npSender;
	}

	/// <summary>
	/// Classe que recebe uma mensagem ja processada e separada em:
	/// stVerbo: a acao que se deseja, i.e., "examinar", "pegar", etc
	/// stParam1:	primeiro parametro para o verbo acima. Por exemplo, em "examinar", podemos examinar algo
	/// stParam2:	segundo parametro. Por exemplo, "cochichar" precisa de 2 parametros
	/// nParam:		numero de parametros enviados aqui, usado para verificacao
	/// eVerb:		qual dos verbos validos esta na mensagem
	/// </summary>
	class MessageMud
	{
		public string stVerbo;
		public string stParam1;
		public string stParam2;
		public int nParam;
		public MudVerbs eVerb;
	}

	private int nGlobalId;
	// Usado para guardar um numero unico para as mensagens
	private MudServer scriptServer;
	// Aponta para o script que executa o servidor
	// Lista de mensagens recebidas do servidor
	private List<MessageEntry> listaDeMensagens = new List<MessageEntry>();
	// Lista de Comandos (a.k.a. mensagens já separadas)
	private List<MudCommands> mudCommands = new List<MudCommands>();

	// Lista de jogadores na sessão
	private List<GameObject> listPlayers = new List<GameObject>();

	// Sala inicial
	private MudCRoom startRoom;

	// Use this for initialization
	void Start()
	{
		
		nGlobalId = 0;
		
		// Encontra a sala inicial na hierarquia
		startRoom = GameObject.Find("Room1").GetComponent<MudCRoom>();
		
		// Preenche a lista com todos os comandos possíveis
		MudCommands mudCommand = new MudCommands();
		
		// Aponta para o script do servidor
		scriptServer = gameObject.GetComponent<MudServer>();
		//	MudServer scriptServer = gameObject.GetComponent("MudServer") as MudServer;
		
		// FIXME:
		// Examinar
		mudCommand.eVerb = MudVerbs.examinar;
		mudCommands.Add(mudCommand);
		
	}

	// Update is called once per frame
	void Update()
	{
		
		// Processa a lista de mensagens
		if(listaDeMensagens.Count != 0) {
			listaDeMensagens.ForEach(ProcessMessage);
			listaDeMensagens.Clear();
		}
	}


	/*---------------------------------------------------------------------------------------------------------*/
	/*
	 * 										Funções do Jogador
	 */
	/*---------------------------------------------------------------------------------------------------------*/

	/// <summary>
	/// Cria uma nova instância de Player e o adiciona à sala inicial
	/// </summary>
	/// <param name="stPlayerName"> Nome do jogador
	/// </param>
	public string AddNewPlayer(NetworkPlayer npPlayer, string stPlayerName)
	{
		
		// 1 - Adicionar um novo jogador ao mundo
		// 2 - Colocar ele na sala inicial
		// 3 - Enviar uma mensagem de boas vindas para seu cliente
		
		// Cria uma nova instância de Player
		GameObject tempPlayer = Instantiate(Resources.Load("Player")) as GameObject;
		tempPlayer.name = stPlayerName;
		// Troca o nome do objeto para o nome do player
		// Preenche a descrição com Examinaro nome do jogador
		tempPlayer.GetComponent<MudCPlayer>().SetDescription(stPlayerName);
		Debug.Log("AddNewPlayer: getcomponent: " + tempPlayer.GetComponent<MudCPlayer>());
		// Guarda o identificador de rede
		tempPlayer.GetComponent<MudCPlayer>().SetNetworkPlayer(npPlayer);
		// DEBUG
		Debug.Log("AddNewPlayer| npPlayer: " + npPlayer);
		
		// Adiciona o jogador à lista de jogadores do jogo
		listPlayers.Add(tempPlayer);
		
		// Posiciona o jogador na sala inicial
		tempPlayer.GetComponent<MudCPlayer>().SetRoom(startRoom);
		// Executa o examinar obrigatório
		string stWelcomeMsg = startRoom.Examinar(tempPlayer.GetComponent<MudCPlayer>());
		
		return stWelcomeMsg;
	}
		/****************************************************************************************************************/		
		// TESTES!!!
		// TODO
		/* 1 - A sala consegue dizer que está nela? Imprescindível para que seja possível dar uma examinar
		 * na sala -> OK!!!
		 */		
		/*	foreach(GameObject itPlayer in listPlayers) {
				Debug.Log("Player esta na sala " + itPlayer.GetComponent<MudCPlayer>().roomIn);	
			
			if(itPlayer.GetComponent<MudCPlayer>().roomIn == startRoom) {
				Debug.Log("Esta na sala correta...");
			}
			else {
				Debug.Log("Deu porcaria...");	
			}
		}
		*/		
		/* 2 - fazer um pick de alguma coisa garante que vou ter acesso à ela? Por exemplo, fazer um pick na chave
		 * deve retirá-la da sala e colocá-la no player --> OK!!!
		 */		
		// Dá o pick na chave
		/*		MudCGenericGameObject itemPego = startRoom.GetComponent<MudCGenericGameObject>().ObjectsIn[0]; // Tá ruim isso
		Debug.Log("Tentando pegar " + itemPego);
		
		// Testar se é pickable
		if(itemPego.GetComponent<MudCGenericGameObject>().Pickable) {
			// Passa a chave para o player
			tempPlayer.GetComponent<MudCPlayer>().ObjectsIn.Add(itemPego);
			// Remove da Sala
			startRoom.GetComponent<MudCGenericGameObject>().ObjectsIn.Remove(itemPego);
		}
	*/		
		
	
						/*
	 * Funções de Comunicação e Processamento de Mensagens
	 */

	/// <summary>
	/// Recebe uma mensagem do servidor e adiciona-a em uma lista, para posterior processamento
	/// </summary>
	/// <param name="stTimeStamp"> string com o horário da mensagem
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="stPlayer"> Nome do jogador
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="stMsg"> Mensagem
	/// A <see cref="System.String"/>
	/// </param>
	public void AddNewMessage(string stTimeStamp, string stPlayer, string stMsg)
	{
		
		MessageEntry msgRecebida = new MessageEntry();
		
		msgRecebida.nId = nGlobalId;
		msgRecebida.stTime = stTimeStamp;
		msgRecebida.stSender = stPlayer;
		msgRecebida.stMsg = stMsg;
		// FIXME: estamos obtendo o network player buscando na hierarquia por seu nome. Não tem jeito melhor?
		msgRecebida.npSender = GameObject.Find(stPlayer).GetComponent<MudCPlayer>().networkPlayer;
		
		// Adiciona a mensagem recebida à lista de mensagens
		listaDeMensagens.Add(msgRecebida);
		
		// FIXME: é necessário mesmo este contador?
		// Incrementa o contador global de mensagens
		nGlobalId++;
	}

	/*
	 * @brief	Faz o processamento da mensagem em si
	 * @param
	 * @return	void
	 */
	private void ProcessMessage(MessageEntry message)
	{
		
		// Primeiro passo: quebrar a mensagem em 3 pedacos:
		MessageMud mudCommand = new MessageMud();
		
		// 1 - Verbo, ou seja, a ação que o jogador deseja executar
		string[] stWords = message.stMsg.Split(' ');
		
		int nIdx = 0;
		foreach(string stWord in stWords) {
			
			if(nIdx == 0)
				mudCommand.stVerbo = stWord;
			
			if(nIdx == 1)
				mudCommand.stParam1 = stWord;
			// Primeiro parâmetro, se houver
			if(nIdx >= 2)
				// Segundo parâmetro, se houver
				mudCommand.stParam2 += stWord + " ";
			
			nIdx++;
		}
		
		mudCommand.nParam = ((nIdx <= 2) ? nIdx : 2);
		
		// DEBUG
		Debug.Log("Verbo: " + mudCommand.stVerbo + " P1: " + mudCommand.stParam1 + " P2: " + mudCommand.stParam2);
		
		PreProcessCommand(message, mudCommand);
	}


	/*
	 * @brief	Pré-processa a mensagem quebrada em parâmetros: verifica se o verbo está no dicionário, se o
	 * número de parâmetros está correto, etc
	 * @param	
	 * @return
	 */
	private void PreProcessCommand(MessageEntry msgEntry, MessageMud mudMsg)
	{
		
		string stVerboP = mudMsg.stVerbo.ToLower();
		string stReturnMsg = "";
		// Mensagem de resposta a ser enviada ao cliente
		// Obter o player que enviou este comando
		MudCPlayer senderPlayer = GameObject.Find(msgEntry.stSender).GetComponent<MudCPlayer>();
		// Descobrir qual sala ele está
		MudCRoom roomIn = senderPlayer.roomIn;
		
		// Examinar
		if(stVerboP == "examinar" || stVerboP == "exam" || stVerboP == "ex" || stVerboP == "e") {
			
			mudMsg.eVerb = MudVerbs.examinar;
			stReturnMsg += ProcessaExaminar(mudMsg, roomIn, senderPlayer);
		}
		
		// Mover
		if(stVerboP == "mover" || stVerboP == "mv" || stVerboP == "m") {
			
			mudMsg.eVerb = MudVerbs.mover;
			
			stReturnMsg += ProcessaMover(mudMsg, roomIn, senderPlayer);
		}
		
		// Pegar
		if(stVerboP == "pegar" || stVerboP == "p") {
			
			mudMsg.eVerb = MudVerbs.pegar;
			
			stReturnMsg += ProcessaPegar(mudMsg, roomIn, senderPlayer);
		}
		
		// Largar
		if(stVerboP == "largar" || stVerboP == "l") {
			
			mudMsg.eVerb = MudVerbs.largar;
			
			stReturnMsg += ProcessaLargar(mudMsg, roomIn, senderPlayer);
		}

		// Inventario
		if(stVerboP == "inventorio" || stVerboP == "inv" || stVerboP == "inventario" || stVerboP == "i") {
			
			mudMsg.eVerb = MudVerbs.inventorio;
			
			stReturnMsg += ProcessaInventario(mudMsg, roomIn, senderPlayer);
		}
		
		// Usar
		if(stVerboP == "usar" || stVerboP == "u") {
			
			mudMsg.eVerb = MudVerbs.usar;
			
			stReturnMsg += ProcessaUsar(mudMsg, roomIn, senderPlayer);
		}
		
		// Falar
		if(stVerboP == "falar" || stVerboP == "f") {
			
			mudMsg.eVerb = MudVerbs.falar;
			
			stReturnMsg += ProcessaFalar(mudMsg, roomIn, senderPlayer);
		}		
	
		// Cochichar
		if(stVerboP == "cochichar" || stVerboP == "c") {
			
			mudMsg.eVerb = MudVerbs.cochichar;
			
			stReturnMsg += ProcessaCochichar(mudMsg, roomIn, senderPlayer);
		}

		// Ajuda
		if(stVerboP == "ajuda" || stVerboP == "a") {
			
			// Enviar mensagem com os comandos disponíveis
			mudMsg.eVerb = MudVerbs.ajuda;
			
			stReturnMsg += ProcessaAjuda(mudMsg, roomIn, senderPlayer);
		}
		
		// Ok, agora devolve a mensagem para o player que enviou o comando
		Debug.Log("Deveria estar mandando: " + stReturnMsg);
		scriptServer.SendChatMessageTo(msgEntry.npSender, stReturnMsg);
		
	}


	/***********************************************************************************************************/
	/*
	 * Processamento dos Verbos/Comandos
	 */
	/***********************************************************************************************************/

	/// <summary>
	/// Processa o comando examinar
	/// </summary>
	/// <param name="mudMsg">
	/// MudMsg com a mensagem enviada já quebrada em partes <see cref="MessageMud"/>
	/// </param>
	/// <param name="roomIn">
	/// Objeto que representa a sala que o player está <see cref="MudCRoom"/>
	/// </param>
	/// <returns>
	/// String com a descricao da execucao de examinar, ou texto com erro <see cref="System.String"/>
	/// </returns>
	private string ProcessaExaminar(MessageMud mudMsg, MudCRoom roomIn, MudCPlayer senderPlayer)
	{
		
		string stReturnMsg = "";
		
		if(mudMsg.nParam == 1) {
			
			// Obtém a descricão da sala
			stReturnMsg += roomIn.GetComponent<MudCRoom>().Examinar(senderPlayer);
		}
		
		// TODO:
		// Implementar examinar para um objeto qualquer (nParam = 2)
		
		// Implementar examinar com erro (objeto inexiste, número de parâmetros inválidos, etc);
		
		
		
		return stReturnMsg;
	}

	/// <summary>
	/// Processa o comando mover
	/// </summary>
	/// <param name="mudMsg">
	/// A <see cref="MessageMud"/>
	/// </param>
	/// <param name="roomIn">
	/// A <see cref="MudCRoom"/>
	/// </param>
	/// <param name="senderPlayer">
	/// A <see cref="MudCPlayer"/>
	/// </param>
	/// <returns>
	/// A <see cref="System.String"/>
	/// </returns>
	private string ProcessaMover(MessageMud mudMsg, MudCRoom roomIn, MudCPlayer senderPlayer)	{
		
		string stReturnMsg = "";
		
		if(mudMsg.nParam == 1) {
			
			// Mover sem parâmetros: deve retornar um mensagem de erro
			stReturnMsg += "Nao foi especificada nenhuma direcao.";
		} 
		else if(mudMsg.nParam == 2) {
			
			// Verificar se o parâmetro está ok...
			string stDirection = ProcessDirectionString(mudMsg.stParam1);
			
			if(stDirection != "None") {
				
				// 1 - Nesta sala, o que há nesta direcão?
				MudCDoor door = WhatIsInThisDirection(roomIn, stDirection);
				if(door != null) {
					
					// Há uma porta nesta direcao
					if(door.GetComponent<MudCDoor>().Locked) {
						
						// Porta trancada
						stReturnMsg += "A porta " + door.name + " esta trancada.";
					} 
					else {
						
						// Porta destrancada
						foreach(MudCRoom novaSala in door.GetComponent<MudCDoor>().ObjectsIn) {
							
							if(novaSala != roomIn) {
								
								// Troca o player de sala
								senderPlayer.GetComponent<MudCPlayer>().SetRoom(novaSala);
								// Executa o 'Examinar' obrigatório
								stReturnMsg += novaSala.Examinar(senderPlayer);
							}
							
						}
					}
				} 
				else {
					// Informa ao jogador que não é possível se mover para cá...
					stReturnMsg += "Nao e' possivel mover-se para " + stDirection + ", nao ha porta ou passagem ali. ";
				}
			}
			else {
				
				// Direcao invalida
				stReturnMsg += "A direcao informada nao e' valida.";
			}
		} 
		else {
			
			// Número de parametros inválidos, avisar ao jogador
			stReturnMsg += "Não foi especificada uma direcão válida.";
		}
	
		return stReturnMsg;
	}
	
	/// <summary>
	/// Processa o comando pegar.
	/// O jogador pode pegar quaisquer objetos que sejam do tipo "item" e estejam com "Pickable" true.
	/// </summary>
	/// <param name="mudMsg">
	/// A <see cref="MessageMud"/>
	/// </param>
	/// <param name="roomIn">
	/// A <see cref="MudCRoom"/>
	/// </param>
	/// <param name="senderPlayer">
	/// A <see cref="MudCPlayer"/>
	/// </param>
	/// <returns>
	/// A <see cref="System.String"/>
	/// </returns>
	private string ProcessaPegar(MessageMud mudMsg, MudCRoom roomIn, MudCPlayer senderPlayer) {
		
		string stReturnMsg = "";
		
		if(mudMsg.nParam == 1) {
			
			// pegar sem parâmetros: deve retornar um mensagem de erro
			stReturnMsg += "Nao ha' nenhum objeto para pegar";
		}
		else if (mudMsg.nParam == 2) {
			
			if(roomIn.ObjectsIn.Count != 0) {
				
				// Ok, o nome do objeto pode ser algo tipo 'meu nome'. Quando processado pelo MUD,
				// as 2 palavras que compõem o nome ficam separadas. Aqui nós juntamos elas novamente.
				string stObjectNameInMudMsg = (mudMsg.stParam1 + mudMsg.stParam2);
				// Limpa qualquer espaco que tenha sobrado
				stObjectNameInMudMsg = stObjectNameInMudMsg.Replace(" ","");
				// Converte para lower case para facilitar a digitacão
				stObjectNameInMudMsg = stObjectNameInMudMsg.ToLower();
				
				foreach(MudCGenericGameObject objeto in roomIn.ObjectsIn) {
					
					Debug.Log("PROCESSAPEGAR| Comparando '" + objeto.name.ToLower() + "' com '" + stObjectNameInMudMsg + "'");
					if(objeto.name.ToLower() == stObjectNameInMudMsg) {
						
						// Ok, objeto encontrado! Verifica se realmente ele é um item e se é coletável
						if(objeto.Pickable && objeto.Type == MudCGenericGameObject.eObjectType.Item) {
							
							// Coloca o objeto no inventário do player...
							senderPlayer.ObjectsIn.Add(objeto);
							// e retira da sala
							roomIn.ObjectsIn.Remove(objeto);
							// e cai fora do laco!
							stReturnMsg += "Objeto '" + objeto.Name + "' adicionado ao seu inventario. ";
							break;
						}
						else {
							
							stReturnMsg += "Nao e' possivel carregar este objeto!";
						}
					}
				}
			}
			else {
				
				stReturnMsg += "Nao ha objetos nesta sala.";
			}
		}
		else {
			
			stReturnMsg += "Objeto invalido para o comando pegar. Verifique.";
		}
		
		return stReturnMsg;
	}
	
	/// <summary>
	/// Processa o comando largar
	/// </summary>
	/// <param name="mudMsg">
	/// MudMsg com a mensagem enviada já quebrada em partes <see cref="MessageMud"/>
	/// </param>
	/// <param name="roomIn">
	/// Objeto que representa a sala que o player está <see cref="MudCRoom"/>
	/// </param>
	/// <returns>
	/// String com a descricao da execucao de examinar, ou texto com erro <see cref="System.String"/>
	/// </returns>
	private string ProcessaLargar(MessageMud mudMsg, MudCRoom roomIn, MudCPlayer senderPlayer)
	{
	
		string stReturnMsg = "";
		bool bnDropped = false;
		
		if(mudMsg.nParam == 1) {
			// Somente o comando
			stReturnMsg += "Voce deve especificar qual objeto deseja largar.";
		}
		
		else {
		
			// TODO:
			// 1 - Verificar se o jogador possui algum objeto
			// 2 - Verificar se o objeto que ele deseja largar esta em seu inventario
			// 3 - Retirar o objeto do inventario
			// 4 - Adicionar o objeto aos objetos da sala
			// 5 - Avisar a todos os jogadores desta acão
			if(senderPlayer.ObjectsIn.Count != 0 ) {
				
				// Ok, o nome do objeto pode ser algo tipo 'meu nome'. Quando processado pelo MUD,
				// as 2 palavras que compõem o nome ficam separadas. Aqui nós juntamos elas novamente.
				string stObjectNameInMudMsg = (mudMsg.stParam1 + mudMsg.stParam2);
				// Limpa qualquer espaco que tenha sobrado
				stObjectNameInMudMsg = stObjectNameInMudMsg.Replace(" ","");
				// Converte para lower case para facilitar a digitacão
				stObjectNameInMudMsg = stObjectNameInMudMsg.ToLower();
				
				foreach(MudCGenericGameObject objeto in senderPlayer.ObjectsIn) {

					Debug.Log("PROCESSALARGAR| Comparando '" + objeto.name.ToLower() + "' com '" + stObjectNameInMudMsg + "'");
					if(objeto.name.ToLower() == stObjectNameInMudMsg) {
						
						roomIn.ObjectsIn.Add(objeto);
						senderPlayer.ObjectsIn.Remove(objeto);
						bnDropped = true; // Indica que largamos o objeto
						stReturnMsg += "Objeto '" + objeto.Name + "' largado na sala.";
						break;
					}
				}
				
				if(!bnDropped) {
					
					// Nao largamos objeto algum, sinal que nao possuimos o objeto solicitado
					stReturnMsg += "Voce nao possui o objeto '" + mudMsg.stParam1 + " " + mudMsg.stParam2 + "' para largar.";
				}
			}
			else {
				
				stReturnMsg += "Voce nao possui nenhum objeto!";
			}
		}
		
		return stReturnMsg;
	}

/// <summary>
	/// Processa o comando para verificar o inventario do jogador
	/// </summary>
	/// <param name="mudMsg">
	/// MudMsg com a mensagem enviada já quebrada em partes <see cref="MessageMud"/>
	/// </param>
	/// <param name="roomIn">
	/// Objeto que representa a sala que o player está <see cref="MudCRoom"/>
	/// </param>
	/// <returns>
	/// String com a descricao da execucao de examinar, ou texto com erro <see cref="System.String"/>
	/// </returns>
	private string ProcessaInventario(MessageMud mudMsg, MudCRoom roomIn, MudCPlayer senderPlayer)
	{
	
		string stReturnMsg = "";
		
		if(senderPlayer.ObjectsIn.Count != 0) {
			
			stReturnMsg += "Voce esta carregando os seguintes objetos:\n";
			
			foreach(MudCGenericGameObject objeto in senderPlayer.ObjectsIn) {
				
				stReturnMsg += "- " + objeto.Name + ": " + objeto.Description + "\n";
			}
		}
		else {
			
			stReturnMsg += "Voce nao possui objetos em seu inventario.";
		}
		
		
		return stReturnMsg;
	}

/// <summary>
	/// Processa o comando usar
	/// </summary>
	/// <param name="mudMsg">
	/// MudMsg com a mensagem enviada já quebrada em partes <see cref="MessageMud"/>
	/// </param>
	/// <param name="roomIn">
	/// Objeto que representa a sala que o player está <see cref="MudCRoom"/>
	/// </param>
	/// <returns>
	/// String com a descricao da execucao de examinar, ou texto com erro <see cref="System.String"/>
	/// </returns>
	private string ProcessaUsar(MessageMud mudMsg, MudCRoom roomIn, MudCPlayer senderPlayer)
	{
	
		// TODO: implementar!
		
		string stReturnMsg = "";
		
		if(mudMsg.nParam == 1) {
			// Somente o comando
		}
		else if(mudMsg.nParam == 2) {
			// Comando e 1 parâmetro
		}
		else {
			// Comando e vários parâmetros
			
		}
		
		return stReturnMsg;
	}
		
/// <summary>
	/// Processa o comando falar
	/// </summary>
	/// <param name="mudMsg">
	/// MudMsg com a mensagem enviada já quebrada em partes <see cref="MessageMud"/>
	/// </param>
	/// <param name="roomIn">
	/// Objeto que representa a sala que o player está <see cref="MudCRoom"/>
	/// </param>
	/// <returns>
	/// String com a descricao da execucao de examinar, ou texto com erro <see cref="System.String"/>
	/// </returns>
	private string ProcessaFalar(MessageMud mudMsg, MudCRoom roomIn, MudCPlayer senderPlayer)
	{
	
		// TODO: implementar!
		
		string stReturnMsg = "";
		
		if(mudMsg.nParam == 1) {
			// Somente o comando
		}
		else if(mudMsg.nParam == 2) {
			// Comando e 1 parâmetro
		}
		else {
			// Comando e vários parâmetros
			
		}
		
		return stReturnMsg;
	}
		
/// <summary>
	/// Processa o comando cochichar
	/// </summary>
	/// <param name="mudMsg">
	/// MudMsg com a mensagem enviada já quebrada em partes <see cref="MessageMud"/>
	/// </param>
	/// <param name="roomIn">
	/// Objeto que representa a sala que o player está <see cref="MudCRoom"/>
	/// </param>
	/// <returns>
	/// String com a descricao da execucao de examinar, ou texto com erro <see cref="System.String"/>
	/// </returns>
	private string ProcessaCochichar(MessageMud mudMsg, MudCRoom roomIn, MudCPlayer senderPlayer)
	{
	
		// TODO: implementar!
		
		string stReturnMsg = "";
		
		if(mudMsg.nParam == 1) {
			// Somente o comando
		}
		else if(mudMsg.nParam == 2) {
			// Comando e 1 parâmetro
		}
		else {
			// Comando e vários parâmetros
			
		}
		
		return stReturnMsg;
	}

/// <summary>
	/// Processa o comando ajuda
	/// </summary>
	/// <param name="mudMsg">
	/// MudMsg com a mensagem enviada já quebrada em partes <see cref="MessageMud"/>
	/// </param>
	/// <param name="roomIn">
	/// Objeto que representa a sala que o player está <see cref="MudCRoom"/>
	/// </param>
	/// <returns>
	/// String com a descricao da execucao de examinar, ou texto com erro <see cref="System.String"/>
	/// </returns>
	private string ProcessaAjuda(MessageMud mudMsg, MudCRoom roomIn, MudCPlayer senderPlayer)
	{
	
		// TODO: implementar!
		
		string stReturnMsg = "";
		
		if(mudMsg.nParam == 1) {
			// Somente o comando
		}
		else if(mudMsg.nParam == 2) {
			// Comando e 1 parâmetro
		}
		else {
			// Comando e vários parâmetros
			
		}
		
		return stReturnMsg;
	}
		
	
	/// <summary>
	/// Recebe uma string com uma direcão e retorna ela "normalizada"
	/// </summary>
	private string ProcessDirectionString(string stDir)
	{
		
		stDir = stDir.ToLower();
		
		if(stDir == "n" || stDir == "norte") {
			
			return "Norte";
		}
		
		if(stDir == "s" || stDir == "sul") {
			
			return "Sul";
		}
		
		if(stDir == "e" || stDir == "leste") {
			
			return "Leste";
		}
		
		if(stDir == "o" || stDir == "oeste") {
			
			return "Oeste";
		}
		
		// Direcão inválida
		return "None";
	}

	/***********************************************************************************************************/
	/*
	 * Funcões auxiliares
	 */
	/***********************************************************************************************************/

	/// <summary>
	/// Retorna a porta definida em determinada direcão
	/// </summary>
	private MudCDoor WhatIsInThisDirection(MudCGenericGameObject room, string stDirection)
	{
		
		if(stDirection == "Norte") {
			return room.GetComponent<MudCRoom>().doorN;
		}
		
		if(stDirection == "Leste") {
			return room.GetComponent<MudCRoom>().doorE;
		}
		
		if(stDirection == "Oeste") {
			return room.GetComponent<MudCRoom>().doorO;
		}
		
		if(stDirection == "Sul") {
			return room.GetComponent<MudCRoom>().doorS;
		}
		
		return null;
	}



	/// <summary>
	/// Retorna a lista de jogadores em determinada sala, com a excecão do próprio player
	/// </summary>
	/// <param name="room">
	/// Sala que se deseja saber quem está lá <see cref="MudCRoom"/>
	/// </param>
	/// <param name="playerMe">
	/// Jogador a se excluir da lista (o próprio player em geral) <see cref="MudCPlayer"/>
	/// </param>
	/// <returns>
	/// Uma lista com players <see cref="List<MudCPlayer>"/>
	/// </returns>
	public List<MudCPlayer> PlayersInARoomExceptMe(MudCRoom room, MudCPlayer playerMe)
	{
		
		List<MudCPlayer> playersInRoom = new List<MudCPlayer>();
		
		foreach(GameObject player in listPlayers) {
			
			if(player.GetComponent<MudCPlayer>().roomIn == room) {
				
				if(player.GetComponent<MudCPlayer>() != playerMe) {
					playersInRoom.Add(player.GetComponent<MudCPlayer>());
					Debug.Log("Adicionando: " + playersInRoom.Count);
				}
			}
		}
		
		return playersInRoom;
	}
	
	
}
