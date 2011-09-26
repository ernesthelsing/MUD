using UnityEngine;
using System.Collections.Generic;

public class MudCGenericGameObject : MonoBehaviour {

	/// <summary>
	/// Aqui sao definidos todos os objetos comuns a todas as classes do MUD.
	/// </summary>
	
	// Tipos possíveis de objetos
	public enum eObjectType { Room, Player, Item, Door };
	// Posicões possíveis para portas e objetos na sala
	public enum ePosition { Norte, Leste, Oeste, Sul, None };
	
	public string Name; // Nome do objeto
	public string Description; // Descrição do objeto quando se dá 'examinar' nele;
	public bool Pickable; // Indica se o objeto é carregável ou não
	public eObjectType Type; // Tipo do objeto
	public ePosition Position; // Posicão do objeto na sala
	public List<MudCGenericGameObject> ObjectsIn; // Objetos que estão 'dentro' deste objeto. Podem ser players dentro da sala, objetos com o player, etc
		
	// Use this for initialization
	void Start () {

	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	public void SetDescription(string stDescription) {
		
		this.Description = stDescription;
	}
	
	/// <summary>
	/// Retorna uma string com a posicão atual do objeto na sala
	/// </summary>
	/// <returns>
	/// A <see cref="System.String"/>
	/// </returns>
	public string GetPosition(){
		
		string[] stPositions = { "Norte", "Leste", "Oeste", "Sul", "None" };
		int nIdxInEnum = (int)Position;
		
		return stPositions[nIdxInEnum];

	}
	
	/// <summary>
	/// Altera a posição do objeto
	/// </summary>
	/// <param name="stNewPos">
	/// A <see cref="System.String"/>
	/// </param>
	public void SetPosition(string stNewPos) {
		
		// 
		if(stNewPos == "Norte") {
			
			Position = ePosition.Norte;
			return;
		}
		if(stNewPos == "Leste") {
			
			Position = ePosition.Leste;
			return;
		}
		if(stNewPos == "Oeste") {
			
			Position = ePosition.Oeste;
			return;
		}
		if(stNewPos == "Sul") {
			
			Position = ePosition.Sul;
			return;
		}
		
	}
}
