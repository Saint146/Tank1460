using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using Tank1460.Common.Extensions;
using Tank1460.Input;

namespace Tank1460.Forms;

internal abstract class Form
{
    public FormStatus Status { get; private set; }

    protected IReadOnlyDictionary<int, FormItem> Items => _items;

    private readonly Dictionary<int, FormItem> _items = new();

    protected static readonly Color DefaultTextNormalColor = Color.White;
    protected static readonly Color DefaultTextShadowColor = new(0xff7f7f7f);
    protected static readonly Color DefaultTextPressedColor = new(0x775000e0);

    protected ContentManagerEx Content;

    private bool _wasMouseDown;

    /// <summary>
    /// Элемент, на котором левую кнопку мыши последний раз зажали.
    /// </summary>
    private FormItem _lastPressedItem;

    /// <summary>
    /// Элемент, над которым находится мышь.
    /// </summary>
    private FormItem _hoveringItem;

    protected Form(ContentManagerEx content)
    {
        Content = content;
        Status = FormStatus.Running;
    }

    public void HandleInput(PlayerInputCollection playersInputs, MouseState mouseState)
    {
        if (Status is not FormStatus.Running)
            return;

        foreach (var (playerIndex, playerInputs) in playersInputs)
        {
            if (playerInputs.Pressed != PlayerInputCommands.None)
                OnPress(playerIndex, playerInputs.Pressed);
        }

        _hoveringItem = HitTest(mouseState.Position);

        var isMouseDown = mouseState.LeftButton == ButtonState.Pressed;
        switch (_wasMouseDown)
        {
            case false when isMouseDown:
                // Клавишу только что нажали.
                _wasMouseDown = true;

                _lastPressedItem = _hoveringItem;
                break;

            case true when !isMouseDown:
                // Клавишу только что отпустили.
                _wasMouseDown = false;

                if (_lastPressedItem is not null && _lastPressedItem == _hoveringItem)
                    OnClick(_lastPressedItem);
                else
                    OnClick(null);

                _lastPressedItem = null;
                break;
        }
    }

    public void Update(GameTime gameTime)
    {
        foreach (var (_, item) in _items)
        {
            var itemVisualStatus = _lastPressedItem == item && _hoveringItem == item ? FormItemVisualStatus.Pressed :
                _lastPressedItem == item || _hoveringItem == item && _lastPressedItem is null ? FormItemVisualStatus.Hover : FormItemVisualStatus.Normal;

            item.SetStatus(itemVisualStatus);
            item.Update(gameTime);
        }

        OnUpdate(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var (_, item) in _items)
            if (item.Visible)
                item.Draw(spriteBatch);
    }

    protected abstract void OnUpdate(GameTime gameTime);

    protected abstract void OnClick([CanBeNull] FormItem item);

    protected abstract void OnPress(PlayerIndex playerIndex, PlayerInputCommands input);

    protected void Exit()
    {
        Status = FormStatus.Exited;
    }

    protected void AddItem(FormItem item, Point? position = null)
    {
        // TODO: Может нафиг эти ключи вообще.
        _items.Add(_items.Count, item);
        if (position.HasValue)
            item.Position = position.Value;
    }

    protected FormImage CreateTextImage(string text, Font font = null)
    {
        font ??= Content.LoadFont(@"Sprites/Font/Pixel8");
        var itemSize = font.GetTextSize(text);

        var templateTexture = Content.LoadNewSolidColoredTexture(Color.Transparent, itemSize.X, itemSize.Y);
        var texture = templateTexture.Copy();
        texture.Draw(font.CreateTexture(text), Point.Zero);

        var animation = new Animation(texture, new[] { double.MaxValue }, false);
        return new FormImage(animation);
    }

    protected FormButton CreateTextButton(string text,
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
                                                                  texture.Draw(normalFont.CreateTexture(text), halfMargins);
                                                                  return texture;
                                                              });

        var hoverTexture = Content.LoadOrCreateCustomTexture($"{text}|hover",
                                                             () =>
                                                             {
                                                                 var texture = templateTexture.Copy();
                                                                 texture.Draw(shadowFont.CreateTexture(text),
                                                                              halfMargins + new Point(1, 1));
                                                                 texture.Draw(normalFont.CreateTexture(text), halfMargins);
                                                                 return texture;
                                                             });

        var pressedTexture = Content.LoadOrCreateCustomTexture($"{text}|pressed",
                                                               () =>
                                                               {
                                                                   var texture = templateTexture.Copy();
                                                                   texture.Draw(shadowFont.CreateTexture(text),
                                                                                halfMargins + new Point(1, 1));
                                                                   texture.Draw(pressedFont.CreateTexture(text), halfMargins);
                                                                   return texture;
                                                               });

        return new FormButton(normalTexture, hoverTexture, pressedTexture, new[] { double.MaxValue });
    }

    [CanBeNull]
    private FormItem HitTest(Point mousePosition)
    {
        return _items.Values.FirstOrDefault(item => item.Bounds.Contains(mousePosition));
    }
}