﻿global using PlayerInputCollection = System.Collections.Generic.Dictionary<Microsoft.Xna.Framework.PlayerIndex, Tank1460.PlayerInput.PlayerInputs>;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tank1460.Extensions;
using Tank1460.SaveLoad.Settings;

namespace Tank1460.PlayerInput;

internal class PlayerInputHandler
{
    private readonly PlayerIndex[] _allPlayers;

    // Мапы, использующиеся в процессе игры.
    private readonly Dictionary<int, PlayerIndex> _gamePadIndexesAssignedToPlayers = new();
    private Dictionary<PlayerIndex, Dictionary<Buttons, PlayerInputs>> _gamePadBindings = new();
    private Dictionary<Keys, (PlayerIndex PlayerIndex, PlayerInputs Inputs)> _keyboardBindings = new();

    // Мапы, использующиеся для хранения настроек.
    private Dictionary<PlayerIndex, KeyboardControlsSettings> _keyboardControlsByPlayer;

    public PlayerInputHandler(PlayerIndex[] allPlayers)
    {
        _allPlayers = allPlayers;
    }

    internal ICollection<int> GetActiveGamePadIndexes() => _gamePadIndexesAssignedToPlayers.Keys;

    public PlayerInputCollection HandleInput(KeyboardState keyboardState, Dictionary<int, GamePadState> gamePadStates)
    {
        var playersInputs = new PlayerInputCollection();
        foreach (var playerIndex in _allPlayers)
            playersInputs[playerIndex] = PlayerInputs.None;

        // Нажатые клавиши можем получить целиком и обработать их.
        foreach (var key in keyboardState.GetPressedKeys())
        {
            if (_keyboardBindings.TryGetValue(key, out var binding))
                playersInputs[binding.PlayerIndex] |= binding.Inputs;
        }

        // С геймпадами придется наоборот - перебирать все забинженные клавиши.
        foreach (var (gamePadIndex, gamePadState) in gamePadStates)
        {
            var playerIndex = _gamePadIndexesAssignedToPlayers[gamePadIndex];
            var bindings = _gamePadBindings[playerIndex];
            foreach (var (button, input) in bindings)
            {
                if (gamePadState.IsButtonDown(button))
                    playersInputs[playerIndex] |= input;
            }
        }

        return playersInputs;
    }

    public void LoadControlSettings(ControlsSettings userSettings)
    {
        // Считываем настройки клавиатуры.
        _keyboardControlsByPlayer = (userSettings?.PlayerControls).EmptyIfNull()
                                                                 .Where(controls => controls.Keyboard is not null)
                                                                 .ToDictionary(controls => controls.PlayerIndex, controls => controls.Keyboard);

        // Если игрок остался без настроек, а у нас для него есть дефолтные, прописываем их ему.
        foreach (var playerIndex in _allPlayers.Where(player => !_keyboardControlsByPlayer.ContainsKey(player)))
        {
            var controls = InputDefaults.GetDefaultPlayersKeyboardBindings(playerIndex);
            if (controls is not null)
                _keyboardControlsByPlayer[playerIndex] = controls;
        }

        _keyboardBindings = ConvertBindingsToKeysMap(_keyboardControlsByPlayer);

        // Теперь к геймпадам.
        _gamePadIndexesAssignedToPlayers.Clear();
        var allConnectedGamepads = GamePadExtensions.GetAllConnectedGamepads()
                                                    .ToDictionary(x => x.Value.Identifier, x => x.Key);

        // Приписываем геймпады игрокам в соответствии с настройками.
        foreach (var controls in (userSettings?.PlayerControls).EmptyIfNull())
        {
            if (!Enum.IsDefined(controls.PlayerIndex))
                continue;

            foreach (var gamepadId in controls.GamePadIds.EmptyIfNull())
            {
                if (!allConnectedGamepads.TryGetValue(gamepadId, out var gamepadIndex)) continue;

                // TODO: Хранить полную инфу по геймпадам в течение всей игры, чтобы записать весь список геймпадов каждого игрока при выходе.
                _gamePadIndexesAssignedToPlayers[gamepadIndex] = controls.PlayerIndex;
                Debug.WriteLine($"Assigned gamepad #{gamepadIndex} to player #{controls.PlayerIndex}");
                break;
            }
        }

        // Приписываем игрокам, оставшимся без геймпадов, те, что могли остаться.
        foreach (var playerIndex in _allPlayers.Where(player => !_gamePadIndexesAssignedToPlayers.ContainsValue(player)))
        {
            if (!allConnectedGamepads.Values
                                     .TryGetFirst(out var firstFreeGamepadIndex, gamePadIndex => !_gamePadIndexesAssignedToPlayers.ContainsKey(gamePadIndex)))
                continue;

            _gamePadIndexesAssignedToPlayers[firstFreeGamepadIndex] = playerIndex;
            Debug.WriteLine($"Auto-assigned gamepad #{firstFreeGamepadIndex} to player #{playerIndex}");
        }

        // TODO: Бинды геймпадов пока всегда дефолтные и не хранятся в настройках.
        _gamePadBindings = _gamePadIndexesAssignedToPlayers.ToDictionary(x => x.Value, _ => InputDefaults.GetDefaultGamepadBindings());
    }

    private static Dictionary<Keys, (PlayerIndex PlayerIndex, PlayerInputs Inputs)> ConvertBindingsToKeysMap(
        Dictionary<PlayerIndex, KeyboardControlsSettings> keyboardControlsByPlayer)
    {
        var bindings = new Dictionary<Keys, (PlayerIndex PlayerIndex, PlayerInputs Inputs)>();
        // Конвертируем настройки в мапу по клавишам.
        bindings.Clear();
        foreach (var (playerIndex, controls) in keyboardControlsByPlayer)
        {
            foreach (var binding in controls.Bindings)
            {
                foreach (var key in binding.Keys)
                    bindings[key] = (playerIndex, binding.Command);
            }
        }

        return bindings;
    }

    public PlayerControlSettings[] SaveSettings()
    {
        return _keyboardControlsByPlayer.Select(x => new PlayerControlSettings
        {
            PlayerIndex = x.Key,
            Keyboard = x.Value
        }).ToArray();
    }
}