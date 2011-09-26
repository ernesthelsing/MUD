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
	
		int nCount = 0;
		string stAux = "Esta sala tem ";
		string stDoors = "";
		
		if(doorN) {
			
			nCount++;
			stDoors += "ao norte";
		}
		
		if(doorE) {
			
			if(nCount != 0) {
				// Já temos a descricão de uma porta. Então colocamos uma vírgula e a próxima descricão
				stDoors += ", ";
			}
			nCount++;
			stDoors +="ao leste";
		}
		
		if(doorO) {
			
			if(nCount != 0) {
				// Já temos a descricão de uma porta. Então colocamos uma vírgula e a próxima descricão
				stDoors += ", ";
			}
			nCount++;
			stDoors +="a oeste";
		}
		
		if(doorS) {
			
			if(nCount != 0) {
				// Já temos a descricão de uma porta. Então colocamos uma vírgula e a próxima descricão. Como sul
				// é a última, colocamos o 'e' também
				stDoors += ", e ";
			}
			nCount++;
			stDoors +="ao sul";
		}
		
		// Colocamos o ponto final
		stDoors += ".";
		
		// Acertos finais na frase...
		if(nCount == 1) {
		
			// Somente uma porta na sala
			stAux += "uma porta ";
		}
		else {
		
			// Mais de uma porta na sala
			stAux += "portas ";
		}
		
		// Ok, montamos a frase completa
		return stAux + stDoors + " ";
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
