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

}
