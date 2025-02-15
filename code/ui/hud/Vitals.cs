﻿// Copyright (c) 2022 Ape Tavern, do not share, re-distribute or modify
// without permission of its author (insert_email_here)

using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace Fortwars;

public class Vitals : Panel
{
	public Vitals()
	{
		StyleSheet.Load( "/ui/hud/Vitals.scss" );

		_ = new VitalsPanel( this, "add_box" );
	}

	class VitalsPanel : Panel
	{
		public string PlayerHealth => Local.Pawn.Health.CeilToInt().ToString();

		public VitalsPanel( Panel parent, string icon )
		{
			var iconPanel = Add.Icon( icon );
			var label = Add.Label( "100" );
			label.Bind( "text", this, nameof( PlayerHealth ) );

			Parent = parent;
		}
	}
}
