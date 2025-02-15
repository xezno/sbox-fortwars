// Copyright (c) 2022 Ape Tavern, do not share, re-distribute or modify
// without permission of its author (insert_email_here)

using Sandbox;
using System.Linq;

namespace Fortwars;

public partial class FortwarsPlayer : Sandbox.Player
{
	[Net] public string Killer { get; set; }
	[Net] public string SkinMaterialPath { get; set; }

	public DamageInfo LastDamage { get; private set; }
	public ClothingContainer Clothing = new();
	public ClothingContainer CleanClothing = new();

	[ConVar.Server( "fw_time_between_spawns", Help = "How long do players need to wait between respawns", Min = 1, Max = 30 )]
	public static int TimeBetweenSpawns { get; set; } = 10;

	public bool IsSpectator
	{
		get => Team == null;
	}

	public FortwarsPlayer()
	{
		Inventory = new Inventory( this );
	}

	public FortwarsPlayer( Client cl ) : this()
	{
		// Load clothing from client data
		CleanClothing.LoadFromClient( cl );

		// Get skin material.. this is a bit shit
		SkinMaterialPath = CleanClothing.Clothing.Select( x => x.SkinMaterial ).Where( x => !string.IsNullOrWhiteSpace( x ) ).FirstOrDefault();
	}

	public override void Respawn()
	{
		// assign random team
		if ( Team == null )
		{
			int team = Client.All.Count % 2;
			if ( team == 0 )
				Team = Game.Instance.BlueTeam;
			else
				Team = Game.Instance.RedTeam;
		}

		SetModel( "models/playermodel/playermodel.vmdl" );

		// Allow Team class to dress the player
		if ( Team != null )
		{
			Team.OnPlayerSpawn( this );
		}

		if ( IsSpectator )
		{
			EnableAllCollisions = false;
			EnableDrawing = false;

			Controller = null;
			CameraMode = new SpectateRagdollCamera();

			base.Respawn();

			return;
		}

		Controller = new FortwarsWalkController();
		Animator = new FortwarsPlayerAnimator();
		CameraMode = new FirstPersonCamera();

		EnableAllCollisions = true;
		EnableDrawing = true;

		// Draw clothes etc
		foreach ( var child in Children )
			child.EnableDrawing = true;

		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		DressPlayerClothing();

		Game.Instance.Round.SetupInventory( this );

		InSpawnRoom = true;

		base.Respawn();
	}

	public void DressPlayerClothing()
	{

		Clothing.ClearEntities();

		Clothing = new ClothingContainer();

		foreach ( var item in CleanClothing.Clothing )
		{
			Clothing.Clothing.Add( item );
		}


		foreach ( var item in Class.Cosmetics )
		{
			Clothing cloth = ResourceLibrary.Get<Clothing>( item );
			Clothing.Clothing.RemoveAll( x => !x.CanBeWornWith( cloth ) );
			Clothing.Clothing.Add( cloth );
		}

		Clothing.DressEntity( this );
	}

	public override void OnKilled()
	{
		BecomeRagdollOnClient( Position,
					 Rotation,
					 Velocity,
					 LastDamage.Flags,
					 LastDamage.Position,
					 LastDamage.Force.Normal * 1024,
					 GetHitboxBone( LastDamage.HitboxIndex ) );

		base.OnKilled();
		RespawnTimer = TimeBetweenSpawns;

		Inventory.DropActive();

		//
		// Delete any items we didn't drop
		//
		Inventory.DeleteContents();

		Controller = null;
		CameraMode = new SpectateRagdollCamera();

		EnableAllCollisions = false;
		EnableDrawing = false;

		// Don't draw clothes etc
		foreach ( var child in Children )
			child.EnableDrawing = false;
	}

	[Net] public TimeUntil RespawnTimer { get; set; }

	public override void Simulate( Client cl )
	{
		if ( LifeState == LifeState.Dead )
		{
			if ( RespawnTimer <= 0 && IsServer )
			{
				Respawn();
			}

			return;
		}

		var controller = GetActiveController();
		controller?.Simulate( cl, this, GetActiveAnimator() );

		if ( Input.ActiveChild != null )
		{
			ActiveChild = Input.ActiveChild;
		}

		SimulateActiveChild( cl, ActiveChild );
		SimulateDeployables( cl );

		if ( LifeState != LifeState.Alive )
			return;

		TickPlayerUse();
	}

	private void SimulateDeployables( Client cl )
	{
		Entity.All
			.OfType<Deployable>()
			.Where( x => x.Owner == this )
			.ToList()
			.ForEach( x => x.Simulate( cl ) );
	}

	protected override void TickPlayerUse()
	{
		// This is serverside only
		if ( !Host.IsServer ) return;

		// Turn prediction off
		using ( Prediction.Off() )
		{
			if ( Input.Pressed( InputButton.Use ) )
			{
				Using = FindUsable();

				if ( Using == null )
				{
					if ( ActiveChild is not PhysGun )
						UseFail();
					return;
				}
			}

			if ( !Input.Down( InputButton.Use ) )
			{
				StopUsing();
				return;
			}

			if ( !Using.IsValid() )
				return;

			// If we move too far away or something we should probably ClearUse()?

			//
			// If use returns true then we can keep using it
			//
			if ( Using is IUse use && use.OnUse( this ) )
				return;

			StopUsing();
		}
	}

	public void Reset()
	{
		Host.AssertServer();

		DressPlayerClothing();

		Health = 100;
		Game.Instance.MoveToSpawnpoint( this );
	}

	public override void TakeDamage( DamageInfo info )
	{
		LastDamage = info;

		bool isHeadshot = (HitboxIndex)info.HitboxIndex == HitboxIndex.Head;

		if ( isHeadshot )
			info.Damage *= 2.0f;

		if ( info.Attacker is FortwarsPlayer attacker && attacker != this )
		{
			if ( attacker.TeamID == TeamID )
				return; // No team damage

			Killer = attacker.Client.Name;

			// Note - sending this only to the attacker!
			attacker.DidDamage( To.Single( attacker ), info.Position, isHeadshot, info.Damage, ( (float)Health ).LerpInverse( 100, 0 ) );
		}
		else
		{
			Killer = null;
		}

		base.TakeDamage( info );

		if ( info.Weapon.IsValid() || info.Attacker.IsValid() )
			TookDamage( To.Single( Client ), info.Weapon.IsValid() ? info.Weapon.Position : info.Position );
	}

	[ClientRpc]
	public void DidDamage( Vector3 pos, bool isHeadshot, float amount, float healthinv )
	{
		Sound.FromScreen( "dm.ui_attacker" ).SetVolume( 0.5f );
		Hitmarker.Instance.OnHit( amount, isHeadshot );
	}

	[ClientRpc]
	public void TookDamage( Vector3 pos )
	{
		DamageIndicator.Current?.OnHit( pos );
	}

	TimeSince timeSinceLastFootstep = 0;

	public override void OnAnimEventFootstep( Vector3 pos, int foot, float volume )
	{
		if ( LifeState != LifeState.Alive )
			return;

		if ( !IsClient )
			return;

		if ( timeSinceLastFootstep < 0.2f )
			return;

		// Don't play if sliding
		if ( Controller is FortwarsWalkController { DuckSlide: { IsActiveSlide: true } } )
			return;

		// These are super quiet unless we bump them up. This might be due to some volume
		// bug in-engine, I don't know, I don't care.
		volume *= 5f;

		timeSinceLastFootstep = 0;

		var tr = Trace.Ray( pos, pos + Vector3.Down * 20 )
			.Radius( 1 )
			.Ignore( this )
			.Run();

		if ( !tr.Hit ) return;

		tr.Surface.DoFootstep( this, tr, foot, volume );
	}
}
