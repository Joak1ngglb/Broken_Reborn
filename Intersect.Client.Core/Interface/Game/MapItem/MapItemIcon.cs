using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Items;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Core;

namespace Intersect.Client.Interface.Game.Inventory;

public partial class MapItemIcon
{
    public ImagePanel Container;

    public MapItemInstance? MyItem;

    public Guid MapId;

    public int TileIndex;

    public ImagePanel Pnl;

    private ImagePanel _rarityBorderTop;
    private ImagePanel _rarityBorderBottom;
    private ImagePanel _rarityBorderLeft;
    private ImagePanel _rarityBorderRight;

    private MapItemWindow mMapItemWindow;

    public MapItemIcon(MapItemWindow window)
    {
        mMapItemWindow = window;
    }

    public void Setup()
    {
        Pnl = new ImagePanel(Container, "MapItemIcon");
        Pnl.HoverEnter += pnl_HoverEnter;
        Pnl.HoverLeave += pnl_HoverLeave;
        Pnl.Clicked += pnl_Clicked;

        _rarityBorderTop = new ImagePanel(Pnl, "RarityBorderTop") { Texture = Graphics.Renderer.WhitePixel, IsVisibleInParent = false };
        _rarityBorderBottom = new ImagePanel(Pnl, "RarityBorderBottom") { Texture = Graphics.Renderer.WhitePixel, IsVisibleInParent = false };
        _rarityBorderLeft = new ImagePanel(Pnl, "RarityBorderLeft") { Texture = Graphics.Renderer.WhitePixel, IsVisibleInParent = false };
        _rarityBorderRight = new ImagePanel(Pnl, "RarityBorderRight") { Texture = Graphics.Renderer.WhitePixel, IsVisibleInParent = false };
    }

    void pnl_Clicked(Base sender, MouseButtonState arguments)
    {
        if (MyItem == null || TileIndex < 0 || TileIndex >= Options.Instance.Map.MapWidth * Options.Instance.Map.MapHeight)
        {
            return;
        }

        _ = Player.TryPickupItem(MapId, TileIndex, MyItem.Id);
    }

    void pnl_HoverLeave(Base sender, EventArgs arguments)
    {
        Interface.GameUi.ItemDescriptionWindow?.Hide();
    }

    void pnl_HoverEnter(Base? sender, EventArgs? arguments)
    {
        if (MyItem == null)
        {
            return;
        }

        if (InputHandler.MouseFocus != null)
        {
            return;
        }

        if (Globals.InputManager.IsMouseButtonDown(MouseButton.Left))
        {
            return;
        }

        Interface.GameUi.ItemDescriptionWindow?.Show(ItemDescriptor.Get(MyItem.ItemId), MyItem.Quantity, MyItem.ItemProperties);
    }

    private void UpdateRarityBorder(ItemDescriptor? descriptor)
    {
        if (descriptor == null || descriptor.Rarity <= 0 || !CustomColors.Items.Rarities.TryGetValue(descriptor.Rarity, out var color))
        {
            _rarityBorderTop.IsVisibleInParent = false;
            _rarityBorderBottom.IsVisibleInParent = false;
            _rarityBorderLeft.IsVisibleInParent = false;
            _rarityBorderRight.IsVisibleInParent = false;
            return;
        }

        const int border = 2;
        _rarityBorderTop.SetBounds(-border, -border, Pnl.Width + border * 2, border);
        _rarityBorderBottom.SetBounds(-border, Pnl.Height, Pnl.Width + border * 2, border);
        _rarityBorderLeft.SetBounds(-border, -border, border, Pnl.Height + border * 2);
        _rarityBorderRight.SetBounds(Pnl.Width, -border, border, Pnl.Height + border * 2);

        _rarityBorderTop.RenderColor = color;
        _rarityBorderBottom.RenderColor = color;
        _rarityBorderLeft.RenderColor = color;
        _rarityBorderRight.RenderColor = color;

        _rarityBorderTop.IsVisibleInParent = true;
        _rarityBorderBottom.IsVisibleInParent = true;
        _rarityBorderLeft.IsVisibleInParent = true;
        _rarityBorderRight.IsVisibleInParent = true;
    }

    public FloatRect RenderBounds()
    {
        var rect = new FloatRect()
        {
            X = Pnl.ToCanvas(new Point(0, 0)).X,
            Y = Pnl.ToCanvas(new Point(0, 0)).Y,
            Width = Pnl.Width,
            Height = Pnl.Height
        };

        return rect;
    }

    public void Update()
    {
        if (MyItem == null)
        {
            return;
        }

        var item = ItemDescriptor.Get(MyItem.ItemId);
        if (item != null)
        {
            var itemTex = Globals.ContentManager.GetTexture(Framework.Content.TextureType.Item, item.Icon);
            if (itemTex != null)
            {
                Pnl.RenderColor = item.Color;
                Pnl.Texture = itemTex;
                UpdateRarityBorder(item);
            }
            else
            {
                if (Pnl.Texture != null)
                {
                    Pnl.Texture = null;
                    UpdateRarityBorder(null);
                }
            }
        }
        else
        {
            if (Pnl.Texture != null)
            {
                Pnl.Texture = null;
                UpdateRarityBorder(null);
            }
        }
    }
}
