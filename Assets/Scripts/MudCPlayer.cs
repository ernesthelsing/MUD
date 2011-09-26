using UnityEngine;
using System.Collections;

public class MudCPlayer : MudCGenericGameObject {

	// Variáveis da classe
	public NetworkPlayer networkPlayer;
	public MudCRoom roomIn; // Em qual sala estou?
	
	// Use this for initializationGame
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	public void SetRoom(MudCRoom newRoom) {
		
		this.roomIn = newRoom;
	}
	
	public void SetNetworkPlayer(NetworkPlayer networkPlayer) {
		
		this.networkPlayer = networkPlayer;	
	}
		
}
