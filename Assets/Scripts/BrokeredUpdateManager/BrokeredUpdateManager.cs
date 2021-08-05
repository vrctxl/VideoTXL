/*
	BrokeredUpdatManager - 2021 <>< CNLohr
	
	This file may be copied freely copied and modified under the MIT/x11
	license.
	
	How to:
		* Make sure to have the VRC SDK 3.0 and UdonSharp installed.
		* Make an empty, somewhere called "BrokeredUpdateManager"
		* Add this file as an Udon Sharp Behavior to that object.
		
	Usage:
		* Call GetIncrementingID() to get a unique ID, just a global counter.
		* Call `RegisterSubscription( this )` / `UnregisterSubscription( this )`
			in order to get 'BrokeredUpdate()` called every frame during
			the period between both calls.
		* Call `RegisterSlowUpdate( this )` / `UnregisterSlowUpdate( this )`
			in order to get `SlowUpdate()` called every several frames.
			(Between the two calls.) All slow updates are called round-robin.
			
	You can get a copy of a references to this object by setting the reference
	in `Start()` i.e.
	
	private BrokeredUpdateManager brokeredManager;

	void Start()
	{
		brokeredManager = GameObject.Find( "BrokeredUpdateManager" )
			.GetComponent<BrokeredUpdateManager>();
	}
*/


using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

// This does not have sync'd variables yet, but if it does, they'll be manual.
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class BrokeredUpdateManager : UdonSharpBehaviour
{
	const int MAX_UPDATE_COMPS = 1000;
	const int MAX_SLOW_ROLL_COMPS = 1000;
	private Component [] updateObjectList;
	private int updateObjectListCount;
	private Component [] slowUpdateList;
	private int slowUpdateListCount;
	private int slowUpdatePlace;
	private bool bInitialized = false;

	private int idIncrementer;
	
	public int GetIncrementingID()
	{
		return idIncrementer++;
	}
	
	void DoInitialize()
	{
		bInitialized = true;
		updateObjectList = new Component[MAX_UPDATE_COMPS];
		updateObjectListCount = 0;
		slowUpdateList = new Component[MAX_SLOW_ROLL_COMPS];
		slowUpdateListCount = 0;
		slowUpdatePlace = 0;
	}

    void Start()
    {
		if( !bInitialized ) DoInitialize();
    }

	public void RegisterSubscription( UdonSharpBehaviour go )
	{
		if( !bInitialized ) DoInitialize();
		if( updateObjectListCount < MAX_SLOW_ROLL_COMPS )
		{
			updateObjectList[updateObjectListCount] = (Component)go;
			updateObjectListCount++;
		}
	}
	
	public void UnregisterSubscription( UdonSharpBehaviour go )
	{
		if( !bInitialized ) DoInitialize();
		int i = Array.IndexOf( updateObjectList, go );
		if( i >= 0 )
		{
			Array.Copy( updateObjectList, i + 1, updateObjectList, i, updateObjectListCount - i );
			updateObjectListCount--;
		}
	}

	public void RegisterSlowUpdate( UdonSharpBehaviour go )
	{
		if( !bInitialized ) DoInitialize();
		if( slowUpdateListCount < MAX_UPDATE_COMPS )
		{
			slowUpdateList[slowUpdateListCount] = (Component)go;
			slowUpdateListCount++;
		}
	}

	public void UnregisterSlowUpdate( UdonSharpBehaviour go )
	{
		if( !bInitialized ) DoInitialize();
		int i = Array.IndexOf( slowUpdateList, go );
		if( i >= 0 )
		{
			Array.Copy( slowUpdateList, i + 1, slowUpdateList, i, slowUpdateListCount - i );
			slowUpdateListCount--;
		}
	}

	public void Update()
	{
		if( !bInitialized ) DoInitialize();
		int i;
		for( i = 0; i < updateObjectListCount; i++ )
		{
			UdonSharpBehaviour behavior = (UdonSharpBehaviour)updateObjectList[i];
			if( behavior != null )
			{
				behavior.SendCustomEvent("BrokeredUpdate");
			}
		}
		
		if( slowUpdateListCount > 0 )
		{
			UdonSharpBehaviour behavior = (UdonSharpBehaviour)slowUpdateList[slowUpdatePlace];
			if( behavior != null )
			{
				behavior.SendCustomEvent("SlowUpdate");
			}

			slowUpdatePlace++;

			if( slowUpdatePlace >= slowUpdateListCount )
			{
				slowUpdatePlace = 0;
			}
		}
	}
}
