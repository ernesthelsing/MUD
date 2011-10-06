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
		ajuda,
    mapa
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
		
		// Examinar ao entrar na sala
		mudCommand.eVerb = MudVerbs.examinar;
		mudCommands.Add(mudCommand);
		
	}

	// Update is called once per frame
	void Update()
	{
		
		// FIXME: não é melhor usar uma fila? Pode dar conflito na execução dos comandos de clientes diversos...
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
		// Troca o nome do objeto para o nome do player
		tempPlayer.name = stPlayerName;
		// Preenche a descrição com o nome do jogador
		tempPlayer.GetComponent<MudCPlayer>().SetDescription(stPlayerName);
		// Ajusta o nome do jogador
		tempPlayer.GetComponent<MudCPlayer>().Name = stPlayerName;
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
		
		// Avisa ao demais jogadores da sala
		string stMsgToOthers = "Jogador '" + stPlayerName + "' entrou na sala.";
		TellEverybodyElseInThisRoom(startRoom, tempPlayer.GetComponent<MudCPlayer>(), stMsgToOthers);			
		
		return stWelcomeMsg;
	}

		
	
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
		msgRecebida.npSender = GameObject.Find(stPlayer).GetComponent<MudCPlayer>().GetNetworkPlayer();
		
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
				mudCommand.stParam1 = stWord; // Primeiro parâmetro, se houver
			
			if(nIdx >= 2)	{
				mudCommand.stParam2 += stWord + " ";	// Segundo parâmetro, se houver
			}
			
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

    // Mapa
    if(stVerboP == "mapa" || stVerboP == "m") {
      
      // Enviar mensagem com os comandos disponíveis
      mudMsg.eVerb = MudVerbs.ajuda;
      
      stReturnMsg += ProcessaMapa(mudMsg, roomIn, senderPlayer);
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
		else {
			
			string stDirection = ProcessDirectionString(mudMsg.stParam1);
			
			if(stDirection != "None") {
				
				// Opa, o player quer examinar uma direcão
				if(stDirection == "Norte") {
					
					if(roomIn.doorN != null) {
						
						return "Ao " + stDirection + ", " + roomIn.doorN.GetNiceDescription();
					}
					else {
						
						return "Nao ha' nada nesta direcao.";
					}
				}

				if(stDirection == "Sul") {
					
					if(roomIn.doorS != null) {
						
						return "Ao " + stDirection + ", " + roomIn.doorS.GetNiceDescription();
					}
					else {
						
						return "Nao ha' nada nesta direcao.";
					}
				}
				
				if(stDirection == "Oeste") {
					
					if(roomIn.doorO != null) {
						
						return "A " + stDirection + ", " + roomIn.doorO.GetNiceDescription();
					}
					else {
						
						return "Nao ha' nada nesta direcao.";
					}
				}

				if(stDirection == "Leste") {
					
					if(roomIn.doorE != null) {
						
						return "A " + stDirection + ", " + roomIn.doorE.GetNiceDescription();
					}
					else {
						
						return "Nao ha' nada nesta direcao.";
					}
				}
			}
			else {
				
				// Ok, nao foi passada uma direcao... entao pode ser que o jogador queira examinar um item na sala,
				// no seu inventario ou entao outro jogador
				
				// Verifica no meu inventario
				MudCGenericGameObject objetoExam;
				
				// Primeiro procura no inventario do jogador
				objetoExam = FindObjectByNameInMyInventory(senderPlayer, mudMsg.stParam1);
				
				if(objetoExam == null) {
					// Objeto não encontrado no inventario do jogador. Vamos procurar na sala então
					objetoExam = FindObjectByNameInRoom(roomIn, mudMsg.stParam1);
				}

				// Se achou algum objeto, retorna seu nome e descricão
				if(objetoExam != null) {
				
					stReturnMsg += objetoExam.Name + ": " + objetoExam.Description + ".";
					return stReturnMsg;
				}
				
				// Nenhum objeto encontrado. Quem sabe o jogador está dando 'examinar' em outro jogador?
				MudCPlayer playerInRoom;
				
				playerInRoom = FindPlayerByNameInRoom(roomIn, senderPlayer, mudMsg.stParam1);
				
				if(playerInRoom != null) {
					
					stReturnMsg += playerInRoom.Name + ": " + playerInRoom.Description + ".";
					return stReturnMsg;
				}
				
				
				// Não achamos o objeto em lugar algum. Avisar ao jogador
				stReturnMsg += "Nao e' possivel examinar '" + mudMsg.stParam1 + "'. Verifique.";
			}
    }
		
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
						// Avisa os outros jogadores
						string stMsgToOthers = "Jogador '" + senderPlayer.Name + "' deu de cara na porta trancada ao " + stDirection + "."; 
						TellEverybodyElseInThisRoom(roomIn, senderPlayer, stMsgToOthers);							
						
					} 
					else {
						
						// Porta destrancada
						foreach(MudCRoom novaSala in door.GetComponent<MudCDoor>().ObjectsIn) {
							
							if(novaSala != roomIn) {
								
								// Troca o player de sala
								senderPlayer.GetComponent<MudCPlayer>().SetRoom(novaSala);
								// Executa o 'Examinar' obrigatório
								stReturnMsg += novaSala.Examinar(senderPlayer);
								
								// Avisa os outros jogadores que jogador moveu-se...
								string stMsgToOthers = "Jogador '" + senderPlayer.Name + "' moveu-se para " + stDirection + "."; 
								TellEverybodyElseInThisRoom(roomIn, senderPlayer, stMsgToOthers);			
								
								// ... e avisa ao jogadores da nova sala que o jogador entrou
								stMsgToOthers = "Jogador '" + senderPlayer.Name + "' acabou de entrar na sala vindo do " + ReverseDirectionString(stDirection) + ".";
								TellEverybodyElseInThisRoom(novaSala, senderPlayer, stMsgToOthers);			
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
        //bool que verifica se existe tal objeto
				bool contem = false;
				foreach(MudCGenericGameObject objeto in roomIn.ObjectsIn) {
					
					Debug.Log("PROCESSAPEGAR| Comparando '" + objeto.name.ToLower() + "' com '" + stObjectNameInMudMsg + "'");
					if(objeto.name.ToLower() == stObjectNameInMudMsg) {
						contem = true;
						// Ok, objeto encontrado! Verifica se realmente ele é um item e se é coletável
						if(objeto.Pickable && objeto.Type == MudCGenericGameObject.eObjectType.Item) {
							
							// Coloca o objeto no inventário do player...
							senderPlayer.ObjectsIn.Add(objeto);
							// e retira da sala
							roomIn.ObjectsIn.Remove(objeto);
							// e cai fora do laco!
							stReturnMsg += "Objeto '" + objeto.Name + "' adicionado ao seu inventario. ";
							
							// Avisa o restante dos jogadores
							string stMsgToOthers = "Jogador '" + senderPlayer.Name + "' pegou o objeto '" + objeto.Name + "' desta sala."; 
							TellEverybodyElseInThisRoom(roomIn, senderPlayer, stMsgToOthers);							
							break;
						}
						else {
							
							stReturnMsg += "Nao e' possivel carregar este objeto!";
						}
					}
				}
        if(!contem){
          stReturnMsg += "Nesta sala nao existe este item.";
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
						
						// Avisa o restante dos jogadores
						string stMsgToOthers = "Jogador '" + senderPlayer.Name + "' largou o objeto '" + objeto.Name + "' nesta sala."; 
						TellEverybodyElseInThisRoom(roomIn, senderPlayer, stMsgToOthers);
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
			stReturnMsg += "O que voce deseja utilizar?";
		}else {
			// Comando e 1 parâmetro
			
			if(senderPlayer.ObjectsIn.Count != 0 ) {

				// Ok, o nome do objeto pode ser algo tipo 'meu nome'. Quando processado pelo MUD,
				// as 2 palavras que compõem o nome ficam separadas. Aqui nós juntamos elas novamente.
				string stObjectNameInMudMsg = mudMsg.stParam1;
				// Limpa qualquer espaco que tenha sobrado
				stObjectNameInMudMsg = stObjectNameInMudMsg.Replace(" ","");
				// Converte para lower case para facilitar a digitacão
				stObjectNameInMudMsg = stObjectNameInMudMsg.ToLower();
        bool usou = false;
        //se nao contem objeto mostra mensagem
        bool contem = false;
        if(mudMsg.stParam2 != null){
          mudMsg.stParam2 = mudMsg.stParam2.Replace(" ","");
          mudMsg.stParam2 = mudMsg.stParam2.ToLower();
        }else{
          stReturnMsg += "Faltam parametros para a acao 'usar'. Para obter ajuda digite 'ajuda'.";
          return stReturnMsg;
        }
				foreach(MudCGenericGameObject objeto in senderPlayer.ObjectsIn) {
					Debug.Log("PROCESSAUSAR| Comparando '" + objeto.name.ToLower() + "' com '" + stObjectNameInMudMsg + "'");
					if(objeto.name.ToLower() == stObjectNameInMudMsg) {
            contem = true;
            if(stObjectNameInMudMsg.IndexOf("chave")==0){
              //testa se usou a chave
              string stDirection = ProcessDirectionString(mudMsg.stParam2);
              Debug.Log("Direcao: " + stDirection + " Deveria ser: "+ mudMsg.stParam2);
              if(stDirection != "None"){
                //verifica se ha uma porta para usar a chave na direção passada
                MudCDoor door = WhatIsInThisDirection(roomIn, stDirection);
                if(door != null) {
                  string chaveDaPorta = "nada";
                  //se existe chave na porta pega o nome
                  if(door.GetComponent<MudCDoor>().objOpener){
                    chaveDaPorta = door.GetComponent<MudCDoor>().objOpener.name;
                    chaveDaPorta = chaveDaPorta.Replace(" ","");
                    chaveDaPorta = chaveDaPorta.ToLower();
                  }
                  Debug.Log("Chave desta Porta: "+chaveDaPorta);
                  //testa se a chave é dessa porta
                  if(chaveDaPorta == stObjectNameInMudMsg){
                    if(door.GetComponent<MudCDoor>().Locked) {
                      door.GetComponent<MudCDoor>().Locked = false;
                      stReturnMsg += "A porta foi destrancada";
                      usou = true;
                    }else{
                      door.GetComponent<MudCDoor>().Locked = true;
                      stReturnMsg += "A porta foi trancada";
                      usou = true;
                    }
                  }else{
                    stReturnMsg += "Esta chave nao eh desta porta.";
                  }
              }else{
                stReturnMsg += "Nao ha uma porta nesta direcao.";
              }
              }else{
                stReturnMsg += "Direcao invalida.";
              }
              // Avisa o restante dos jogadores
              if(usou){
  						  string stMsgToOthers = "Jogador '" + senderPlayer.Name + "' usou o objeto '" + objeto.Name + "' nesta sala.";
  						  TellEverybodyElseInThisRoom(roomIn, senderPlayer, stMsgToOthers);
              }
  						break;
            }else if(stObjectNameInMudMsg.IndexOf("porrete")==0){
              if(PlayersInARoomExceptMe(roomIn, senderPlayer).Count >0){
                foreach(MudCPlayer playerPorrete in PlayersInARoomExceptMe(roomIn, senderPlayer)){
                  if(playerPorrete.name.ToLower() == mudMsg.stParam2){
                    string stMsgToOthers;
                    if(Random.Range(0, 5) == 1){
                      MudCGenericGameObject objetoPlayerPorrete;
                      objetoPlayerPorrete = playerPorrete.ObjectsIn[Random.Range(0,playerPorrete.ObjectsIn.Count)];
                      stReturnMsg += stMsgToOthers = "O :"+playerPorrete.name+" levou uma porretada do "+senderPlayer.name+
                                     " e dropou o item "+objetoPlayerPorrete.name+" de seu inventario.";
                      roomIn.ObjectsIn.Add(objetoPlayerPorrete);
                      playerPorrete.ObjectsIn.Remove(objetoPlayerPorrete);
  
                    }else{
                      stReturnMsg += stMsgToOthers = senderPlayer.name+" bateu no "+playerPorrete.name;
                      TellEverybodyElseInThisRoom(roomIn, senderPlayer, stMsgToOthers);
                    }
                  }else{
                    stReturnMsg += "Jogador Inexistente.";
                  }
                }
              }else{
                stReturnMsg += "Nesta sala nao ha jogadores.";
              }
              break;
            }
					}
				}
        if(!contem){
          stReturnMsg += "Item inexistente.";
        }
			}else{
        stReturnMsg += "Seu inventorio esta vazio.";
      }
			
			
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
	
		string stReturnMsg = "";
		
		if(mudMsg.nParam == 1) {
			// Somente o comando
			stReturnMsg += "Faltou a mensagem a ser enviada para os outros jogadores...";
		}
		else {
			
			string stMsg = senderPlayer.name + " diz: " + mudMsg.stParam1 + " " + mudMsg.stParam2;
			TellEverybodyElseInThisRoom(roomIn, senderPlayer, stMsg);
			stReturnMsg = "Mensagem '" + stMsg + "' enviada para os outros jogadores desta sala.";
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
      stReturnMsg += "Parametros invalidos.";
		}
		else {
      MudCPlayer player;
      if(GameObject.Find(mudMsg.stParam1)){
        stReturnMsg += "(To)"+mudMsg.stParam1+":"+mudMsg.stParam2;
        player = GameObject.Find(mudMsg.stParam1).GetComponent<MudCPlayer>();
        string msgCochichar = senderPlayer.name+":"+mudMsg.stParam2;
  			scriptServer.SendChatMessageTo(player.GetNetworkPlayer(), msgCochichar);
      }else{
        stReturnMsg += "Jogador nao Existe.";
      }
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
  /// Processa o comando mapa
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
  private string ProcessaMapa(MessageMud mudMsg, MudCRoom roomIn, MudCPlayer senderPlayer)
  {

    // TODO: implementar!

    string stReturnMsg = "";

    if(mudMsg.nParam == 1) {
      stReturnMsg += ""+
   "::::::::::::::::::::::::::::::::::::::::::::::::::::::::"+
   "::       ::       ::       ::       ::       ::       ::"+
   "::       ::       ::       ::       ::       ::       ::"+
   "::       ::       ::       ::       ::       ::       ::"+
   "::       ::       ::       ::       ::       ::       ::"+
   "::       ::       ::       ::       ::       ::       ::"+
   "::       ::       ::       ::       ::       ::       ::"+
   "::::::::::::::::::::::::::::::::::::::::::::::::::::::::"+
   ":::::::::         :::::::::::::::::::         ::::::::::"+
   ":      ::         ::       ::      ::         ::       :"+
   ":      ::         ::       ::      ::         ::       :"+
   ":      ::         ::       ::      ::         ::       :"+
   ":      ::         ::       ::      ::         ::       :"+
   ":      ::         ::       ::      ::         ::       :"+
   ":      ::         ::       ::      ::         ::       :"+
   ":::::::::         :::::::::::::::::::         ::::::::::"+
   ""+
   ":::::::::                                     ::::::::::"+
   ":       :                                     ::       :"+
   ":       :                                     ::       :"+
   ":       :                                     ::       :"+
   ":       :                                     ::       :"+
   ":       :                                     ::       :"+
   ":       :                                     ::       :"+
   ":::::::::                                     ::::::::::";

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
	
	/// <summary>
	/// Informa a direção contrário à direção de stDir. Útil para avisar quando um jogador sai de uma sala e entra em outra 
	/// </summary>
	/// <param name="stDir">
	/// String com uma direção <see cref="System.String"/>
	/// </param>
	/// <returns>
	/// A direção contrária da string passada <see cref="System.String"/>
	/// </returns>
	private string ReverseDirectionString(string stDir) {
		
		if(stDir == "Norte")
			return "Sul";
		
		if(stDir == "Sul")
			return "Norte";
		
		if(stDir == "Oeste")
			return "Leste";
		
		if(stDir == "Leste")
			return "Oeste";
		
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
				}
			}
		}
		
		return playersInRoom;
	}

	/// <summary>
	/// Envia uma mensagem para todos os jogadores que estão em determinada sala. Serve
	/// para avisar aos outros jogadores das ações de determinado jogador
	/// </summary>
	/// <param name="room">
	/// A sala atual <see cref="MudCRoom"/>
	/// </param>
	/// <param name="playerMe">
	/// Jogador (que obviamente não deve receber a própria mensagem) <see cref="MudCPlayer"/>
	/// </param>
	/// <param name="stMsg">
	/// Mensagem a ser enviada para todos os jogadores <see cref="System.String"/>
	/// </param>
	public void TellEverybodyElseInThisRoom(MudCRoom room, MudCPlayer playerMe, string stMsg) {
	
		List<MudCPlayer> playersInRoom = new List<MudCPlayer>();
		
		playersInRoom = PlayersInARoomExceptMe(room, playerMe);
		
		if(playersInRoom.Count != 0) {
			
			foreach(MudCPlayer player in playersInRoom) {
				
				scriptServer.SendChatMessageTo(player.GetNetworkPlayer(), stMsg);
			}
		}
		
	}

	/// <summary>
	/// Faz com que o player largue todos os seus objetos na sala. Útil quando este player
	/// desconecta
	/// </summary>
	/// <param name="roomIn">
	/// Sala em que o player está <see cref="MudCRoom"/>
	/// </param>
	public void PlayerDropAllItens(MudCPlayer playerMe) {

		string stMsgToOthers = "";
		MudCRoom roomIn = playerMe.roomIn;
		MudCGenericGameObject objeto;
		
		if(playerMe.ObjectsIn.Count != 0) {
			
			int nIdx = 0;
			for(nIdx = 0;  nIdx < playerMe.ObjectsIn.Count; nIdx++) {
				
				objeto = playerMe.ObjectsIn[nIdx];
				roomIn.ObjectsIn.Add(objeto);
				playerMe.ObjectsIn.Remove(objeto);
				
				stMsgToOthers = "Jogador '" + playerMe.Name + "' largou o objeto '" + objeto.Name + "' nesta sala.\n"; 
			}
			// Avisa todos
			TellEverybodyElseInThisRoom(roomIn, playerMe, stMsgToOthers);
		}
	}
		
	public void RemovePlayerFromListAndKillIt(MudCPlayer playerDisconnected) {
		
		// Encontra o player na lista
		foreach(GameObject player in listPlayers) {
			
			if(player.GetComponent<MudCPlayer>() == playerDisconnected) {
				
				listPlayers.Remove(player);
				Destroy(player);
				break;
			}
		}
	}
	
	/// <summary>
	/// Verifica o inventário de um jogador em busca do objeto de certo nome. Retorna o objeto caso encontrado,
	/// null caso contrário
	/// </summary>
	/// <param name="playerMe">
	/// A <see cref="MudCPlayer"/>
	/// </param>
	/// <param name="stObjectToSearch">
	/// A <see cref="System.String"/>
	/// </param>
	/// <returns>
	/// A <see cref="MudCGenericGameObject"/>
	/// </returns>
	public MudCGenericGameObject FindObjectByNameInMyInventory(MudCPlayer playerMe, string stObjectToSearch) {
		
		if(playerMe.ObjectsIn.Count != 0 ) {
			
			foreach(MudCGenericGameObject objeto in playerMe.ObjectsIn) {
				
				if(stObjectToSearch.ToLower() == objeto.Name.ToLower()) {
					
					return objeto;
				}
			}
		}
			
		return null;
	}
	
	/// <summary>
	/// Busca por um objeto na sala pelo seu nome. Caso encontrado, retorna o objeto, senão retorna null 
	/// </summary>
	/// <param name="roomIn">
	/// A <see cref="MudCRoom"/>
	/// </param>
	/// <param name="stObjectToSearch">
	/// A <see cref="System.String"/>
	/// </param>
	/// <returns>
	/// A <see cref="MudCGenericGameObject"/>
	/// </returns>
	public MudCGenericGameObject FindObjectByNameInRoom(MudCRoom roomIn, string stObjectToSearch) {
		
		if(roomIn.ObjectsIn.Count != 0 ) {
			
			foreach(MudCGenericGameObject objeto in roomIn.ObjectsIn) {
				
				if(stObjectToSearch.ToLower() == objeto.Name.ToLower()) {
					
					return objeto;
				}
			}
		}
			
		return null;
	}

	/// <summary>
	/// Procura por um jogador na sala pelo seu nome. Se encontrado, retorna o jogador, caso contrário
	/// retorn null
	/// </summary>
	/// <param name="roomIn">
	/// A <see cref="MudCRoom"/>
	/// </param>
	/// <param name="playerMe">
	/// A <see cref="MudCPlayer"/>
	/// </param>
	/// <param name="stObjectToSearch">
	/// A <see cref="System.String"/>
	/// </param>
	/// <returns>
	/// A <see cref="MudCPlayer"/>
	/// </returns>
	public MudCPlayer FindPlayerByNameInRoom(MudCRoom roomIn, MudCPlayer playerMe, string stObjectToSearch) {
		
		List<MudCPlayer> playersInRoom = new List<MudCPlayer>();
		
		playersInRoom = PlayersInARoomExceptMe(roomIn, playerMe);
		
		if(playersInRoom.Count != 0) {
			
			foreach(MudCPlayer player in playersInRoom) {

				if(player.Name.ToLower() == stObjectToSearch.ToLower()) {
					
					return player;
				}
					
			}
		}
		
		return null;
	}
	
}
