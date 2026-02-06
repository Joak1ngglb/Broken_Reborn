using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Core;
using Intersect.Framework.Core.GameObjects.Items;

namespace Intersect.Client.Interface.Game;

public partial class SlotItem : ImagePanel
{
    public readonly int SlotIndex;
    public readonly Draggable Icon;
    protected readonly ContextMenu? _contextMenu;

    private Color _rarityBorderColor = Color.Transparent;
    private bool _showRarityBorder;


    public SlotItem(Base parent, string name, int index, ContextMenu? contextMenu) : base(parent, name)
    {
        SlotIndex = index;

        MinimumSize = new Point(34, 34);
        Margin = new Margin(4);
        MouseInputEnabled = true;

        Icon = new Draggable(this, nameof(Icon))
        {
            MinimumSize = new Point(32, 32),
            MouseInputEnabled = true,
            Alignment = [Alignments.Center],
            HoverSound = "octave-tap-resonant.wav",
        };

        _contextMenu = contextMenu;
    }

    public virtual void Update()
    {
    }


    protected void UpdateRarityBorder(ItemDescriptor? descriptor, bool isDragging)
    {
        if (descriptor == null || isDragging || descriptor.Rarity <= 0)
        {
            _showRarityBorder = false;
            return;
        }

        if (!CustomColors.Items.Rarities.TryGetValue(descriptor.Rarity, out var rarityColor))
        {
            _showRarityBorder = false;
            return;
        }

        _showRarityBorder = true;
        _rarityBorderColor = rarityColor;
    }

    protected void ResetRarityBorder()
    {
        _showRarityBorder = false;
    }

    protected override void Render(Framework.Gwen.Skin.Base skin)
    {
        base.Render(skin);

        if (!_showRarityBorder || _rarityBorderColor == Color.Transparent)
        {
            return;
        }

        var renderer = skin.Renderer;
        var bounds = Icon.RenderBounds;

        renderer.DrawColor = _rarityBorderColor;

        const int borderWidth = 2;

        renderer.DrawFilledRect(new Rectangle(bounds.X - borderWidth, bounds.Y - borderWidth, bounds.Width + borderWidth * 2, borderWidth));
        renderer.DrawFilledRect(new Rectangle(bounds.X - borderWidth, bounds.Y + bounds.Height, bounds.Width + borderWidth * 2, borderWidth));
        renderer.DrawFilledRect(new Rectangle(bounds.X - borderWidth, bounds.Y - borderWidth, borderWidth, bounds.Height + borderWidth * 2));
        renderer.DrawFilledRect(new Rectangle(bounds.X + bounds.Width, bounds.Y - borderWidth, borderWidth, bounds.Height + borderWidth * 2));
    }

    public void OpenContextMenu()
    {
        if (_contextMenu is not { } contextMenu)
        {
            return;
        }

        OnContextMenuOpening(contextMenu);
    }

    public override bool DragAndDrop_CanAcceptPackage(Package package)
    {
        return true;
    }

    protected virtual void OnContextMenuOpening(ContextMenu contextMenu)
    {
        // Display our menu... If we have anything to display.
        if (contextMenu.Children.Count > 0)
        {
            contextMenu.Open(Pos.None);
        }
    }
}
