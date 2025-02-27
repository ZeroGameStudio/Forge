﻿// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Forge.Runtime;

public interface IEntity
{
	object PrimaryKey { get; }
}

public interface IEntity<out TPrimaryKey> : IEntity where TPrimaryKey : notnull
{
	public new TPrimaryKey PrimaryKey { get; }
	object IEntity.PrimaryKey => PrimaryKey;
}


