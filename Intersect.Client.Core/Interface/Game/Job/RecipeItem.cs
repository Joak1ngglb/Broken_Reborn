using Intersect.Client.Core;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.DescriptionWindows;
using Intersect.Framework.Core.GameObjects.Crafting;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Core;

namespace Intersect.Client.Interface.Game.Job
{
    public partial class RecipeItem
    {
        public ImagePanel Container;

        public ItemDescriptionWindow DescWindow;

        public bool IsDragging;

        // Dragging
        private bool mCanDrag;

        // References
        private JobsWindow mJobsWindow;

        private Draggable mDragIcon;

        // Slot info
        CraftingRecipeIngredient mIngredient;

        // Mouse Event Variables
        private bool mMouseOver;

        private int mMouseX = -1;

        private int mMouseY = -1;

        public ImagePanel Pnl;

        private ImagePanel? _rarityBorderTop;
        private ImagePanel? _rarityBorderBottom;
        private ImagePanel? _rarityBorderLeft;
        private ImagePanel? _rarityBorderRight;


        public RecipeItem(JobsWindow skillsWindow, CraftingRecipeIngredient ingredient)
        {
            mJobsWindow = skillsWindow;
            mIngredient = ingredient;
        }

        public void Setup(string name)
        {
            Pnl = new ImagePanel(Container, name);
            Pnl.HoverEnter += pnl_HoverEnter;
            Pnl.HoverLeave += pnl_HoverLeave;

            _rarityBorderTop = new ImagePanel(Pnl, "RarityBorderTop") { Texture = Graphics.Renderer.WhitePixel, IsVisibleInParent = false };
            _rarityBorderBottom = new ImagePanel(Pnl, "RarityBorderBottom") { Texture = Graphics.Renderer.WhitePixel, IsVisibleInParent = false };
            _rarityBorderLeft = new ImagePanel(Pnl, "RarityBorderLeft") { Texture = Graphics.Renderer.WhitePixel, IsVisibleInParent = false };
            _rarityBorderRight = new ImagePanel(Pnl, "RarityBorderRight") { Texture = Graphics.Renderer.WhitePixel, IsVisibleInParent = false };

            UpdateRarityBorder(null);
        }

        public void LoadItem()
        {
            var item = ItemDescriptor.Get(mIngredient.ItemId);

            if (item != null)
            {
                var itemTex = Globals.ContentManager.GetTexture(Framework.Content.TextureType.Item, item.Icon);
                if (itemTex != null)
                {
                    Pnl.Texture = itemTex;
                    Pnl.RenderColor = item.Color;
                    UpdateRarityBorder(item);
                }
                else if (Pnl.Texture != null)
                {
                    Pnl.Texture = null;
                    UpdateRarityBorder(null);
                }
            }
            else if (Pnl.Texture != null)
            {
                Pnl.Texture = null;
                UpdateRarityBorder(null);
            }
        }

        private void UpdateRarityBorder(ItemDescriptor? descriptor)
        {
            if (Pnl == null || _rarityBorderTop == null || _rarityBorderBottom == null || _rarityBorderLeft == null || _rarityBorderRight == null)
            {
                return;
            }

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

        void pnl_HoverLeave(Base sender, EventArgs arguments)
        {
            mMouseOver = false;
            mMouseX = -1;
            mMouseY = -1;
            Interface.GameUi.ItemDescriptionWindow?.Hide();
        }

        void pnl_HoverEnter(Base sender, EventArgs arguments)
        {
            if (InputHandler.MouseFocus != null)
            {
                return;
            }

            mMouseOver = true;
            mCanDrag = true;
            if (Globals.InputManager.IsMouseButtonDown(MouseButton.Left))
            {
                mCanDrag = false;

                return;
            }

            if (mIngredient != null && ItemDescriptor.TryGet(mIngredient.ItemId, out var itemDescriptor))
            {
                Interface.GameUi.ItemDescriptionWindow?.Show(itemDescriptor, mIngredient.Quantity);
            }
        }
    }
}
