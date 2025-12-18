using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Fishing;
using Intersect.Network.Packets.Server;
using Intersect.Server.Entities;
using Intersect.Server.Maps;
using Intersect.Server.Networking;
using Intersect.Utilities;

namespace Intersect.Server.Entities.Events;

public class FishEventServer
{
    #region Vars

    private readonly Player _player;

    private int[]? _fishingPosition;
    private Direction _fishingDirection;
    private Guid _fishingSpotId;
    private long _timerWaitFish;
    private Guid _currentFishId;
    private int _stage;

    #endregion

    public FishEventServer(Player player)
    {
        _player = player;
    }

    #region Stage 0 — грок забрасывает удочку или возвращает её

    public void ServerCastFishingRod(Guid fishingSpotId) //Удочку игрок закинул
    {
        var spot = FishingSpotBase.Get(fishingSpotId);
        if (spot != null && Conditions.MeetsConditionLists(spot.FishingRequirements, _player, null))
        {
            _fishingPosition = new[] { _player.X, _player.Y };
            _fishingDirection = _player.Dir;
            _fishingSpotId = fishingSpotId;
            _stage = 1;

            var timer = Randomization.Next(spot.FishingTimeMin, spot.FishingTimeMax);
            _timerWaitFish = Timing.Global.MillisecondsUtc + timer;
            PacketSender.SendClientResultCastFishingRod(_player, true);
        }
        else
        {
            PacketSender.SendClientResultCastFishingRod(_player, false);
        }
    }

    public void ServerReturnFishingRod() //Удочку игрок вернул
    {
        _stage = 0;
        _fishingSpotId = Guid.Empty;
        _timerWaitFish = 0;
    }

    #endregion

    #region Stage 1 Спустя время игроку отправляется случайная рыба

    private bool WaitingCatchFish() //Ждём рыбу
    {
        if (_fishingPosition != null && (_player.X != _fishingPosition[0] || _player.Y != _fishingPosition[1] ||
                                         _fishingDirection != _player.Dir))
        {
            //Отправить отмену или не стоит.
            _stage = 0;
            _fishingSpotId = Guid.Empty;
            _timerWaitFish = 0;
            return false;
        }

        return Timing.Global.MillisecondsUtc >= _timerWaitFish;
    }

    private Guid GetRandomFish() //Даём рыбу
    {
        var fishingSpot = FishingSpotBase.Get(_fishingSpotId);
        var fishes = new List<FishBase>();

        //Сортируем рыбку по возрастанию на редкость
        var fishesSort = fishingSpot?.SortingFishByRarity(FishingSpotBase.SortByType.INCREASING).ToArray();

        //Проверяем рыбу на соблюдение требований пожарной безопасности
        if (fishesSort != null)
        {
            foreach (var fishGuid in fishesSort)
            {
                var fish = FishBase.Get(fishGuid);
                if (fish != null && Conditions.MeetsConditionLists(fish.FishingRequirements, _player, null))
                {
                    fishes.Add(fish);
                }
            }
        }

        if (fishes.Count == 0)
        {
            return Guid.Empty;
        }

        var totalChance = fishes.Sum(fish => Math.Max(0, fish.chance));

        // If all chances are zero or negative, give each fish an even weight to avoid dead ends
        if (totalChance <= 0)
        {
            totalChance = fishes.Count;
        }

        var roll = Randomization.Next(1, totalChance + 1);
        var accumulated = 0;

        foreach (var fish in fishes)
        {
            var weight = Math.Max(0, fish.chance);
            weight = weight == 0 ? 1 : weight;

            accumulated += weight;

            if (roll <= accumulated)
            {
                return fish.Id;
            }
        }

        return fishes.Last().Id;
    }

    #endregion

    private long _packetTime;
    private bool _isFishingVisual;
    private int _stageVisual;
    private bool _isPressed;
    private bool _oldFishingVisual;
    private int _oldStageVisual;
    private bool _oldPressed;

    public void UpdateVisual(bool isFishingVisual, int stageVisual, bool isPressed)
    {
        _isFishingVisual = isFishingVisual;
        _stageVisual = stageVisual;
        _isPressed = isPressed;
        //Console.Write($"Обновление игрока {_player.Name}\n");
    }

    public void FishingUpdate()
    {
        if (Timing.Global.Milliseconds > _packetTime)
        {
            if (_isFishingVisual != _oldFishingVisual ||
                _stageVisual != _oldStageVisual ||
                _isPressed != _oldPressed)
            {
                PacketSender.SendEntityFishing(_player, _isFishingVisual, _stageVisual, _isPressed);
                _oldFishingVisual = _isFishingVisual;
                _oldStageVisual = _stageVisual;
                _oldPressed = _isPressed;
            }

            _packetTime = Timing.Global.Milliseconds + 100;
        }

        switch (_stage)
        {
            case 1:
                if (WaitingCatchFish())
                {
                    _currentFishId = GetRandomFish();
                    if (_currentFishId != Guid.Empty)
                    {
                        PacketSender.SendClientFish(_player, _currentFishId);
                        _stage = 2;
                    }
                    else
                    {
                        _stage = 0;
                        _fishingSpotId = Guid.Empty;
                        _timerWaitFish = 0;
                        _currentFishId = Guid.Empty;
                    }
                }

                break;
        }
    }

    #region Stage 3 Результат рыбалки

    public void FishingSuccess()
    {
        var fish = FishBase.Get(_currentFishId);
        if (fish == null)
        {
            return;
        }

        if (!_player.TryGiveItem(fish.ItemId, 1))
        {
            if (MapController.TryGetInstanceFromMap(_player.MapId, _player.MapInstanceId, out var instance))
            {
                var item = new Database.Item(fish.ItemId, 1);
                instance.SpawnItem((Framework.Items.IItemSource)_player, _player.X, _player.Y, item, 1, _player.Id);
            }
        }

        if (fish.Event != default)
        {
            _player.EnqueueStartCommonEvent(fish.Event);
        }

        //Console.Write($"{_player.Name} поймал на удочку {FishBase.Get(_currentFishId).Name}.\n");
        _stage = 0;
        _fishingSpotId = Guid.Empty;
        _timerWaitFish = 0;
        _currentFishId = Guid.Empty;
    }

    public void FishingFailed()
    {
        string[] strings =
        {
            "Сорвалась..", "Блин..", "Неудача.."
        };
        var rand = Randomization.Next(0, strings.Length);
        PacketSender.SendChatBubble(_player.Id, _player.MapInstanceId, (int)EntityType.GlobalEntity, strings[rand],
            _player.MapId);
        _stage = 0;
        _fishingSpotId = Guid.Empty;
        _timerWaitFish = 0;
        _currentFishId = Guid.Empty;
        //Console.Write($"{_player.Name} не поймал рыбу.\n");
    }

    #endregion
}
