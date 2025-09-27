using System;
using System.Collections.Generic;
using Intersect.Client.General;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Pets;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Entities;

/// <summary>
///     Represents a pet entity on the client. This entity mirrors most of the behaviour of other global
///     entities while providing additional metadata required to relate it to its owner and descriptor.
/// </summary>
public sealed class Pet : Entity
{
    private const int DefaultAttributeCap = 100;

    private PetDescriptor? _cachedDescriptor;
    private Guid _descriptorId;
    private bool _shouldRefreshSprite = true;
    private string? _lastResolvedSprite;

    private int[] _statPointAllocations = Array.Empty<int>();

    public Pet(Guid id, EntityPacket? packet)
        : base(id, packet, EntityType.Pet)
    {
        mRenderPriority = 2;
    }

    /// <summary>
    ///     Gets the identifier of the player that owns this pet.
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    ///     Gets a value indicating whether the server considers this pet despawnable.
    /// </summary>
    public bool Despawnable { get;  set; } = true;

    /// <summary>
    ///     Gets the behaviour currently reported by the server for the pet.
    /// </summary>
    public PetState Behavior { get; set; } = PetState.Follow;

    /// <summary>
    ///     Gets the descriptor identifier used to spawn this pet.
    /// </summary>
    public Guid DescriptorId
    {
        get => _descriptorId;
        private set
        {
            if (_descriptorId == value)
            {
                return;
            }

            _descriptorId = value;
            _cachedDescriptor = null;
            _shouldRefreshSprite = true;
        }
    }

    /// <summary>
    ///     Gets the descriptor associated with the pet, if it is available in the local cache.
    /// </summary>
    public PetDescriptor? Descriptor
    {
        get
        {
            if (TryResolveDescriptor(out var descriptor) && _shouldRefreshSprite)
            {
                UpdateSpriteFromDescriptor(force: true);
            }

            return descriptor;
        }
    }

    /// <summary>
    ///     Gets the player that owns this pet if the player entity is currently known by the client.
    /// </summary>
    public Player? Owner =>
        OwnerId != Guid.Empty && Globals.TryGetEntity(EntityType.Player, OwnerId, out var entity)
            ? entity as Player
            : null;

    /// <summary>
    ///     Returns <c>true</c> when the supplied player is the pet owner.
    /// </summary>
    public bool IsOwner(Player? player) => player?.Id == OwnerId;

    /// <summary>
    ///     Returns <c>true</c> when the local player owns this pet.
    /// </summary>
    public bool IsOwnedByLocalPlayer => IsOwner(Globals.Me);

    private PetGender _gender = PetGender.Unspecified;

    public PetGender Gender
    {
        get => _gender;
        private set
        {
            if (_gender == value)
            {
                return;
            }

            _gender = value;
            _shouldRefreshSprite = true;
        }
    }

    public long Experience { get; private set; }

    public long ExperienceToNextLevel { get; private set; }

    public int StatPoints { get; private set; }

    public IReadOnlyList<int> StatPointAllocations => _statPointAllocations;

    public int Energy { get; private set; }

    public int MoodValue { get; private set; }

    public PetMood Mood { get; private set; } = PetMood.Content;

    public int Maturity { get; private set; }

    public long CareMilliseconds { get; private set; }

    public int WhimsFulfilled { get; private set; }

    public long LastWhimFulfillmentTicks { get; private set; }

    /// <summary>
    ///     Applies the metadata provided by the server to this pet instance.
    /// </summary>
    /// <param name="ownerId">Identifier of the player that owns the pet.</param>
    /// <param name="descriptorId">Identifier of the descriptor that spawned the pet.</param>
    /// <param name="despawnable">Indicates whether the pet can despawn automatically.</param>
    /// <param name="behavior">Behaviour reported by the server.</param>
    /// <param name="gender">Gender assigned by the server.</param>
    public void ApplyMetadata(
        Guid ownerId,
        Guid descriptorId,
        bool despawnable,
        PetState behavior,
        PetGender gender
    )
    {
        if (behavior is not (PetState.Follow or PetState.Stay or PetState.Defend or PetState.Passive))
        {
            behavior = PetState.Follow;
        }

        var ownerChanged = OwnerId != ownerId;
        var descriptorChanged = DescriptorId != descriptorId;
        _ = despawnable;
        const bool normalizedDespawnable = true;
        var despawnableChanged = Despawnable != normalizedDespawnable;
        var behaviorChanged = Behavior != behavior;
        var genderChanged = Gender != gender;

        OwnerId = ownerId;
        DescriptorId = descriptorId;
        Despawnable = normalizedDespawnable;
        Behavior = behavior;
        Gender = gender;

        if (descriptorChanged || genderChanged || _shouldRefreshSprite)
        {
            UpdateSpriteFromDescriptor(force: descriptorChanged || genderChanged);
        }

        if (ownerChanged || descriptorChanged || despawnableChanged || behaviorChanged || genderChanged)
        {
            Globals.NotifyPetMetadataApplied(this);
        }
    }

    public void ApplyProgress(
        long experience,
        long experienceToNextLevel,
        int statPoints,
        int[]? statPointAllocations,
        int energy,
        int moodValue,
        PetMood mood,
        int maturity,
        long careMilliseconds,
        int whimsFulfilled,
        long lastWhimFulfillmentTicks
    )
    {
        Experience = Math.Max(0, experience);
        ExperienceToNextLevel = Math.Max(-1, experienceToNextLevel);
        StatPoints = Math.Max(0, statPoints);

        if (statPointAllocations == null)
        {
            _statPointAllocations = Array.Empty<int>();
        }
        else
        {
            _statPointAllocations = new int[statPointAllocations.Length];
            Array.Copy(statPointAllocations, _statPointAllocations, statPointAllocations.Length);
        }

        Energy = ClampAttribute(energy, Descriptor?.BaseEnergy ?? DefaultAttributeCap);
        MoodValue = ClampAttribute(moodValue, Descriptor?.BaseMood ?? DefaultAttributeCap);
        Mood = mood;
        Maturity = ClampAttribute(maturity, Descriptor?.BaseMaturity ?? DefaultAttributeCap);
        CareMilliseconds = Math.Max(0, careMilliseconds);
        WhimsFulfilled = Math.Max(0, whimsFulfilled);
        LastWhimFulfillmentTicks = Math.Max(0, lastWhimFulfillmentTicks);

        Globals.NotifyPetProgressApplied(this);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();

        _cachedDescriptor = null;
        OwnerId = Guid.Empty;
        DescriptorId = Guid.Empty;
        Despawnable = true;
        Behavior = PetState.Follow;
        Experience = 0;
        ExperienceToNextLevel = 0;
        StatPoints = 0;
        _statPointAllocations = Array.Empty<int>();
        Energy = 0;
        MoodValue = 0;
        Mood = PetMood.Content;
        Maturity = 0;
        CareMilliseconds = 0;
        WhimsFulfilled = 0;
        LastWhimFulfillmentTicks = 0;
        Gender = PetGender.Unspecified;
        _lastResolvedSprite = null;
        _shouldRefreshSprite = true;
    }

    /// <inheritdoc />
    public override void Load(EntityPacket? packet)
    {
        base.Load(packet);

        if (packet is not PetEntityPacket petPacket)
        {
            return;
        }

        _lastResolvedSprite = Sprite;

        ApplyMetadata(
            petPacket.OwnerId,
            petPacket.DescriptorId,
            petPacket.Despawnable,
            petPacket.Behavior,
            petPacket.Gender
        );
    }

    private static int ClampAttribute(int value, int baseValue) =>
        Math.Clamp(value, 0, Math.Max(DefaultAttributeCap, baseValue));

    private bool TryResolveDescriptor(out PetDescriptor? descriptor)
    {
        if (_cachedDescriptor == null && DescriptorId != Guid.Empty)
        {
            PetDescriptor.Lookup.TryGetValue(DescriptorId, out _cachedDescriptor);
        }

        descriptor = _cachedDescriptor;
        return descriptor != null;
    }

    private void UpdateSpriteFromDescriptor(bool force = false)
    {
        if (!TryResolveDescriptor(out var descriptor))
        {
            _shouldRefreshSprite = true;
            return;
        }

        var resolvedSprite = descriptor.GetSpriteForGender(Gender);
        resolvedSprite ??= string.Empty;

        if (!force && string.Equals(_lastResolvedSprite, resolvedSprite, StringComparison.Ordinal))
        {
            _shouldRefreshSprite = false;
            return;
        }

        _lastResolvedSprite = resolvedSprite;
        Sprite = resolvedSprite;
        _shouldRefreshSprite = false;
    }
}
