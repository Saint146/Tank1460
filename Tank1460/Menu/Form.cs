using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using Tank1460.Common.Extensions;
using Tank1460.Input;

namespace Tank1460.Menu;

internal abstract class Form
{
    protected FormStatus Status { get; private set; }

    protected IReadOnlyDictionary<int, FormItem> Items => _items;

    private readonly Dictionary<int, FormItem> _items = new();

    protected static readonly Color DefaultTextNormalColor = Color.White;
    protected static readonly Color DefaultTextShadowColor = new(0xff7f7f7f);
    protected static readonly Color DefaultTextPressedColor = new(0x775000e0);

    protected ContentManagerEx Content;

    /// <summary>
    /// Элемент, на котором левую кнопку мыши последний раз зажали.
    /// </summary>
    private FormItem _lastPressedItem;

    /// <summary>
    /// Элемент, над которым находится мышь.
    /// </summary>
    private FormItem _hoveringItem;

    protected Form(ContentManagerEx content, int defaultPlayerCount, int defaultLevelNumber)
    {
        Status = FormStatus.Running;
        Content = content;
    }

    public void HandleInput(PlayerInputCollection playersInputs, MouseState mouseState)
    {
        if (Status is not FormStatus.Running)
            return;

        if (playersInputs.Values.Any(inputs => inputs.Pressed.HasFlag(PlayerInputCommands.Down)))
            CursorDown();
        else if (playersInputs.Values.Any(inputs => inputs.Pressed.HasFlag(PlayerInputCommands.Up)))
            CursorUp();
        else if (playersInputs.Values.Any(inputs => inputs.Pressed.HasFlag(PlayerInputCommands.Start)))
            Enter();

        _hoveringItem = HitTest(mouseState.Position);

        var isMouseDown = mouseState.LeftButton == ButtonState.Pressed;
        if (_lastPressedItem is null && isMouseDown)
        {
            // Клавишу только что нажали.
            _lastPressedItem = _hoveringItem;
        }
        else if (_lastPressedItem is not null && !isMouseDown)
        {
            // Клавишу только что отпустили.
            if (_hoveringItem == _lastPressedItem)
                HandleClick(_lastPressedItem);

            _lastPressedItem = null;
        }
    }

    public void Update(GameTime gameTime)
    {
        foreach (var (_, item) in _items)
        {
            var itemVisualStatus = _lastPressedItem == item && _hoveringItem == item ? FormItemVisualStatus.Pressed :
                _lastPressedItem == item || _hoveringItem == item && _lastPressedItem is null ? FormItemVisualStatus.Hover : FormItemVisualStatus.Normal;
            item.Update(gameTime, itemVisualStatus);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var (_, item) in _items)
            item.Draw(spriteBatch);
    }

    protected abstract void CursorUp();

    protected abstract void CursorDown();

    protected abstract void Enter();

    protected abstract void HandleClick(FormItem item);

    protected void Exit()
    {
        Status = FormStatus.Exited;
        Exited?.Invoke();
    }

    protected void AddItem(FormItem item, Point? position = null)
    {
        // TODO: Может нафиг эти ключи вообще.
        _items.Add(_items.Count, item);
        if (position.HasValue)
            item.Position = position.Value;
    }

    protected FormItem CreateTextItem(string text,
                                          Color? normalColor = null,
                                          Color? shadowColor = null,
                                          Color? pressedColor = null,
                                          Point? margins = null)
    {
        var normalFont = Content.LoadFont(@"Sprites/Font/Pixel8", normalColor ?? DefaultTextNormalColor);
        var shadowFont = Content.LoadFont(@"Sprites/Font/Pixel8", shadowColor ?? DefaultTextShadowColor);
        var pressedFont = Content.LoadFont(@"Sprites/Font/Pixel8", pressedColor ?? DefaultTextPressedColor);
        margins ??= normalFont.GetTextSize(" ");
        var halfMargins = margins.Value.Divide(2);

        var itemSize = normalFont.GetTextSize(text) + margins.Value;
        var templateTexture = Content.LoadNewSolidColoredTexture(Color.Transparent, itemSize.X, itemSize.Y);
        var normalTexture = Content.LoadOrCreateCustomTexture($"{text}|normal",
                                                              () =>
                                                              {
                                                                  var texture = templateTexture.Copy();
                                                                  texture.Draw(normalFont.CreateTexture(texture.GraphicsDevice, text), halfMargins);
                                                                  return texture;
                                                              });

        var hoverTexture = Content.LoadOrCreateCustomTexture($"{text}|hover",
                                                             () =>
                                                             {
                                                                 var texture = templateTexture.Copy();
                                                                 texture.Draw(shadowFont.CreateTexture(texture.GraphicsDevice, text),
                                                                              halfMargins + new Point(1, 1));
                                                                 texture.Draw(normalFont.CreateTexture(texture.GraphicsDevice, text), halfMargins);
                                                                 return texture;
                                                             });

        var pressedTexture = Content.LoadOrCreateCustomTexture($"{text}|pressed",
                                                               () =>
                                                               {
                                                                   var texture = templateTexture.Copy();
                                                                   texture.Draw(shadowFont.CreateTexture(texture.GraphicsDevice, text),
                                                                                halfMargins + new Point(1, 1));
                                                                   texture.Draw(pressedFont.CreateTexture(texture.GraphicsDevice, text), halfMargins);
                                                                   return texture;
                                                               });

        return new FormItem(normalTexture, hoverTexture, pressedTexture, new[] { double.MaxValue });
    }

    [CanBeNull]
    private FormItem HitTest(Point mousePosition)
    {
        return _items.Values.FirstOrDefault(item => item.Bounds.Contains(mousePosition));
    }

    public delegate void FormEvent();

    public virtual event FormEvent Exited;

    protected enum FormStatus
    {
        Running,
        Exited
    }
}