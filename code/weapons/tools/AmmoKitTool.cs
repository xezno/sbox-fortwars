﻿// Copyright (c) 2022 Ape Tavern, do not share, re-distribute or modify
// without permission of its author (insert_email_here)

using Sandbox;

namespace Fortwars;

[Library( "ammokittool", Title = "Ammokit" )]
public partial class AmmoKitTool : DropTool
{
	public override void Spawn()
	{
		base.Spawn();

		ViewModelEntity?.SetMaterialGroup( "ammo" );
		SetMaterialGroup( "ammo" );
	}

	public override void SpawnPickup()
	{
		ThrowProjectile( new BigAmmoPickup() );
	}

	public override void Simulate( Client player )
	{
		base.Simulate( player );

		// Why the fuck is this even in simulate to begin with
		if ( IsClient )
		{
			ViewModelEntity?.SetMaterialGroup( "ammo" );
			SetMaterialGroup( "ammo" );
		}
	}
}
