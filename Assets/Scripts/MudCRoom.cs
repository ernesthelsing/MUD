/*
 * Classe que define uma sala do MUD
 */

using UnityEngine;
using System.Collections.Generic;

public class MudCRoom : MudCGenericGameObject {
	
	// Variáveis da classe
	public MudCDoor doorN; // Porta ao norte
	public MudCDoor doorE; // Porta ao leste
	public MudCDoor doorO; // Porta a oeste
	public MudCDoor doorS; // Porta ao sul
	
	void Start() {
		
		// Inicializacão básica
		Type = eObjectType.Room;
		Pickable = false;
	}
	
	/// <summary>
	/// Recebe um comando 'examinar' de alguém. Responde adequadamente, dando a 
	/// descricão desta sala, as portas, os objetos e os jogadores que estão nela
	/// </summary>
	public string Examinar(MudCPlayer playerMe) {
		
		string stExaminar = "";
		// Examinar uma sala:
		// 1 - Descricão da própria sala
		stExaminar += this.Description + " ";
		
		// 2 - Descricão das portas e saídas existentes
		stExaminar += this.CheckDoors();
		
		// 3 - Descricão dos objetos na sala e suas posicões
		stExaminar += this.CheckObjectsIn();
		
		// 4 - Descricão dos jogadores presente na sala
		stExaminar += this.CheckPlayersIn(playerMe);
		
		// 5 - Retorna a descricão completa
		return stExaminar;
	}
	
	/// <summary>
	/// Verifica quais portas estão definidas para esta sala (sempre no mínimo uma).
	/// Depois, monta uma frase descrevendo estas portas e retorna.
	/// </summary>
	/// <returns>
	/// A <see cref="System.String"/>
	/// </returns>
	public string CheckDoors() {
	
		string stReturnMsg = "";
		
		if(doorN) {
			stReturnMsg += "Ao norte, esta sala tem uma porta " + doorN.Description + "(" + doorN.Name +"). Esta porta esta' ";
			if(doorN.Locked) {

				stReturnMsg += "trancada. ";
			}
			else {
				
				stReturnMsg += "destrancada. ";
			}
		}

		if(doorS) {
			stReturnMsg += "Ao sul, esta sala tem uma porta " + doorS.Description + "(" + doorS.Name +"). Esta porta esta' ";
			if(doorS.Locked) {

				stReturnMsg += "trancada. ";
			}
			else {
				
				stReturnMsg += "destrancada. ";
			}
		}
		
		if(doorE) {
			stReturnMsg += "A leste, esta sala tem uma porta " + doorE.Description + "(" + doorE.Name +"). Esta porta esta' ";
			if(doorE.Locked) {

				stReturnMsg += "trancada. ";
			}
			else {
				
				stReturnMsg += "destrancada. ";
			}
		}

		if(doorO) {
			stReturnMsg += "A oeste, esta sala tem uma porta " + doorO.Description + "(" + doorO.Name +"). Esta porta esta' ";
			if(doorO.Locked) {

				stReturnMsg += "trancada. ";
			}
			else {
				
				stReturnMsg += "destrancada. ";
			}
		}

		
		// Ok, montamos a frase completa
		return stReturnMsg;
	}
	
	/// <summary>
	/// Verifica quais objetos estão na sala, dá sua descricão e sua posicão
	/// </summary>
	/// <returns>
	/// A <see cref="System.String"/>
	/// </returns>
	public string CheckObjectsIn() {
	
		string stAux = "";
		int nObjetos = ObjectsIn.Count;
		
		if(nObjetos == 0) {
			
			// Não há objetos na sala
			return "Nao ha objetos na sala. ";	
		}
		else {
			
			// Há objetos na sala
			if(nObjetos == 1) {
				
				stAux += "Aqui esta' o seguinte objeto: ";
			}
			else {
				
				stAux += "Aqui estao os seguintes objetos: ";
			}
			
			foreach(MudCGenericGameObject objeto in ObjectsIn) {

				stAux += "'" + objeto.Name + "'\n";
			}
		}
		
		return stAux;
	}
	
	private string CheckPlayersIn(MudCPlayer playerMe) {
	
		string stAux = "";
		
		// Achar todos os jogadores que estão nesta sala
		mud_regras scriptRegras = GameObject.Find("MUD").GetComponent<mud_regras>();
		List<MudCPlayer> playersInThisRoom = new List<MudCPlayer>();
		playersInThisRoom = scriptRegras.PlayersInARoomExceptMe(this, playerMe);
		
		
		if(playersInThisRoom.Count == 0) {
			
			// Só estou eu...
			stAux += "Nao ha' ninguem nesta sala alem de voce.";
		}
		else {
			
			if(playersInThisRoom.Count == 1) {
			
				stAux += "Nesta sala tambem esta ";
			}
			else {
			
				stAux += "Nesta sala tambem estao ";
			}
			
	
			// Lista o nome dos player
			for(int nIdx=0; nIdx < playersInThisRoom.Count; nIdx++) {
				
				stAux += playersInThisRoom[nIdx].name;
				if(nIdx == playersInThisRoom.Count-1) {
					
					// Último elemento
					stAux += ".";
				}
				else{
					
					stAux += ", ";
				}
			}
			
		}
	
		
		return stAux;
	}
}
