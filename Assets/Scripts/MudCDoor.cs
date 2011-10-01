/*
 * Classe que define uma porta do MUD
 */

using UnityEngine;
using System.Collections.Generic;

public class MudCDoor : MudCGenericGameObject {
	
	// Variáveis da classe
	
	public bool Locked; // Está trancada ou não?
	public MudCGenericGameObject objOpener; // Que objeto pode destrancar estar porta?
	
	void Start() {
		
		Type = eObjectType.Door;
		Pickable = false;
	}
  
  /// <summary>
  /// Retorna frase com a descricão da porta. Útil para os comandos de examinar
  /// </summary>
  /// <returns>
  /// A <see cref="System.String"/>
  /// </returns>
  public string GetNiceDescription() {
    
    string stReturnMsg = "";
		
    stReturnMsg += Description;
		
		if(Name != "") {
			stReturnMsg += " (" + Name +")";	
		}
		
		stReturnMsg += ". Esta porta esta' ";

		if(Locked) {

      stReturnMsg += "trancada. ";
    }
    else {
        
			stReturnMsg += "destrancada. ";
    }
		
		return stReturnMsg;
  }

}
