﻿// Copyright (c) 2022 Ape Tavern, do not share, re-distribute or modify
// without permission of its author (insert_email_here)

namespace Fortwars;

class BlueTeam : BaseTeam
{
	public override Team ID => Team.Blue;
	public override string Name => "Blue Team";
	public override Color Color => Color.Blue;
}
