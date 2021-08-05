
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class BrokeredSync : UdonSharpBehaviour
{
	[UdonSynced] private Vector3    syncPosition;
	[UdonSynced] private bool       syncMoving;
	[UdonSynced] private Quaternion syncRotation;

	public bool bDebug;
	public bool bSnap;
	
	private bool wasMoving;
	private Collider thisCollider;
	private BrokeredUpdateManager brokeredUpdateManager;
	private bool masterMoving;
	private bool firstUpdateSlave;
	private float fDeltaMasterSendUpdateTime;
	
    void Start()
    {
		brokeredUpdateManager = GameObject.Find( "BrokeredUpdateManager" ).GetComponent<BrokeredUpdateManager>();
        thisCollider = GetComponent<Collider>();
		
		if( Networking.IsMaster )
		{
			Networking.SetOwner( Networking.LocalPlayer, gameObject );

			syncPosition = transform.localPosition;
			syncRotation = transform.localRotation;
			syncMoving = false;
		}
		else
		{
			firstUpdateSlave = true;
		}
		wasMoving = false;
		masterMoving = false;
    }

	public void SendMasterMove()
	{
		syncPosition = transform.localPosition;
		syncRotation = transform.localRotation;
		
		//We are being moved.
		RequestSerialization();
	}

    override public void OnPickup ()
    {
		thisCollider.enabled = false;
		brokeredUpdateManager.RegisterSubscription( this );
		Networking.SetOwner( Networking.LocalPlayer, gameObject );
		fDeltaMasterSendUpdateTime = 10;
		syncMoving = true;
		masterMoving = true;
    }

    override public void OnDrop()
    {
		brokeredUpdateManager.UnregisterSubscription( this );
		syncMoving = false;
		thisCollider.enabled = true;
		masterMoving = false;
		SendMasterMove();
    }
	
	public override void OnDeserialization()
	{
		if( firstUpdateSlave )
		{
			transform.localPosition = syncPosition;
			transform.localRotation = syncRotation; 
			firstUpdateSlave = false;
		}
		
		if( bDebug )
		{
			Vector4 col = GetComponent<MeshRenderer>().material.GetVector( "_Color" );
			col.z = ( col.z + 0.01f ) % 1;
			GetComponent<MeshRenderer>().material.SetVector( "_Color", col );
		}

        if (!syncMoving)
        {
            //We were released before we got the update.
            transform.localPosition = syncPosition;
            transform.localRotation = syncRotation;
            wasMoving = false;
            brokeredUpdateManager.UnregisterSubscription(this);
        }

        if ( !masterMoving )
		{
			if( !wasMoving && syncMoving )
			{
				brokeredUpdateManager.RegisterSubscription( this );
                wasMoving = true;
			}
		}
		else
		{
			if( Networking.GetOwner( gameObject ) != Networking.LocalPlayer )
			{
				//Master is moving AND another player has it.
				((VRC_Pickup)(gameObject.GetComponent(typeof(VRC_Pickup)))).Drop();
			}
		}
	}
	
	public void BrokeredUpdate()
	{
		if( masterMoving )
		{
			if( bSnap )
			{
				Vector3 ea = transform.localRotation.eulerAngles;
				transform.localPosition = new Vector3( 
					Mathf.Round( transform.localPosition.x / .35f ) * .35f,
					Mathf.Round( transform.localPosition.y / .35f ) * .35f,
					Mathf.Round( transform.localPosition.z / .35f ) * .35f
				);
				ea.x = Mathf.Round( ea.x / 30.0f ) * 30.0f;
				ea.y = Mathf.Round( ea.y / 30.0f ) * 30.0f;
				ea.z = Mathf.Round( ea.z / 30.0f ) * 30.0f;
				transform.localRotation =  Quaternion.Euler( ea );
			}
			if( bDebug )
			{
				Vector4 col = GetComponent<MeshRenderer>().material.GetVector( "_Color" );
				col.x = ( col.x + 0.01f ) % 1;
				GetComponent<MeshRenderer>().material.SetVector( "_Color", col );
			}
			fDeltaMasterSendUpdateTime += Time.deltaTime;
			
			// Don't send location more than 20 FPS.
			if( fDeltaMasterSendUpdateTime > 0.05f )
			{
				SendMasterMove();
				fDeltaMasterSendUpdateTime = 0.0f;
			}
		}
		else
		{
			//Still moving, make motion slacky.
			
			if( syncMoving )
			{

				if( bDebug )
				{
					Vector4 col = GetComponent<MeshRenderer>().material.GetVector( "_Color" );
					col.y = ( col.y + 0.01f ) % 1;
					GetComponent<MeshRenderer>().material.SetVector( "_Color", col );
				}

				float iir = Mathf.Pow( 0.001f, Time.deltaTime );
				float inviir = 1.0f - iir;
				transform.localPosition = transform.localPosition * iir + syncPosition * inviir;
				transform.localRotation = Quaternion.Slerp( transform.localRotation, syncRotation, inviir ); 
				
				wasMoving = true;
			}
			else if( wasMoving )
			{
				if( !syncMoving )
				{
					//We were released.
					transform.localPosition = syncPosition;
					transform.localRotation = syncRotation;
					wasMoving = false;
					brokeredUpdateManager.UnregisterSubscription( this );
				}
			}
		}
	}
}
