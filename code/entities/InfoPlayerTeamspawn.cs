﻿// Copyright (c) 2022 Ape Tavern, do not share, re-distribute or modify
// without permission of its author (insert_email_here)

using Sandbox;
using SandboxEditor;

namespace Fortwars;

/// <summary>
/// This is where players spawn.
/// </summary>
[Library( "info_player_teamspawn" )]
[Title( "Team Spawn" ), Category( "FortWars" )]
[EditorModel( "models/citizen/citizen.vmdl" )]
[HammerEntity]
public partial class InfoPlayerTeamspawn : Entity
{
	[Property]
	public Team Team { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		Transmit = TransmitType.Never;
	}
}
