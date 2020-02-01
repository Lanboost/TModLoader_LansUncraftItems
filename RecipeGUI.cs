using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace LansUncraftItems
{
    internal sealed class RecipeGUI : UIState
    {
        internal const float vpadding = 10;
        internal const float vwidth = 555;
        internal const float vheight = 400;

        private Vector2 offset;

        private UIPanel rootPanel;
        internal UIGrid recipeList;

        internal Item item;
        internal static List<Recipe> currentRecipes;

        internal RecipeGUI()
        {
            SetPadding(vpadding);
            Width.Set(vwidth, 0f);
            Height.Set(vheight, 0f);
            HAlign = VAlign = 0.5f;
        }

        public override void OnInitialize()
        {
            rootPanel = new UIPanel();
            rootPanel.OnMouseUp += new MouseEvent(DragEnd);
            rootPanel.OnMouseDown += new MouseEvent(DragStart);
            rootPanel.Width.Set(300, 0);
            rootPanel.Height.Set(300, 0);
            rootPanel.CopyStyle(this);
            rootPanel.SetPadding(vpadding);
            Append(rootPanel);

            UIText prompt = new UIText(
                $"Choose a recipe (mouseover for more details)" +
                $"{System.Environment.NewLine}Shift-click to uncraft stack",
                1f, false);
            rootPanel.Append(prompt);

            UIImageButton closeButton = new UIImageButton(
                LansUncraftItems.instance.GetTexture("closeButton"));
            closeButton.OnClick += CloseButton_OnClick;
            closeButton.Width.Set(20f, 0f);
            closeButton.Height.Set(20f, 0f);
            closeButton.Left.Set(rootPanel.Width.Pixels - closeButton.Width.Pixels * 2 - vpadding * 4.75f, 0f);
            closeButton.Top.Set(closeButton.Height.Pixels / 2f, 0f);
            rootPanel.Append(closeButton);

            recipeList = new UIGrid();
            recipeList.Width.Set(500f, 0f);
            recipeList.Height.Set(rootPanel.Height.Pixels - prompt.Height.Pixels - vpadding, 0f);
            recipeList.Left.Set(0f, 0f);
            recipeList.Top.Set(vpadding * 6f, 0f);
            recipeList.SetPadding(0);
            recipeList.Initialize();
            rootPanel.Append(recipeList);
        }

        private void CloseButton_OnClick(UIMouseEvent evt, UIElement listeningElement)
        {
            LansUncraftItems.instance.CloseRecipeGUI(false);
        }

        private void DragEnd(UIMouseEvent evt, UIElement listeningElement)
        {
            Vector2 end = evt.MousePosition;

            rootPanel.Left.Set(end.X - offset.X, 0f);
            rootPanel.Top.Set(end.Y - offset.Y, 0f);

            Recalculate();
        }

        private void DragStart(UIMouseEvent evt, UIElement listeningElement)
        {
            offset = new Vector2(
                evt.MousePosition.X - rootPanel.Left.Pixels,
                evt.MousePosition.Y - rootPanel.Top.Pixels);
        }

        public void ListRecipes(Item item, List<Recipe> recipes)
        {
            // Item can't be deleted in callback, so destroy it now 
            // and spawn a total clone if unsuccessful
            this.item = item.Clone();
            item.TurnToAir();
            recipeList.Clear();

            foreach (Recipe recipe in recipes)
            {
                UIRecipePanel panel = new UIRecipePanel(recipe);
                panel.Width.Set(60f, 0f);
                panel.Height.Set(60f, 0f);
                panel.SetPadding(0);
                Texture2D tex = Main.itemTexture[recipe.requiredItem[0].type];
                // Push larger sprites further into upper-left corner
                float 
                    offsetX = MathHelper.Clamp(tex.Width * .001f, 0f, .5f),
                    offsetY = MathHelper.Clamp(tex.Height * .001f, 0f, .5f);
                UIImageButton btn = new UIImageButton(tex)
                {
                    HAlign = 0.5f - offsetX,
                    VAlign = 0.5f - offsetY
                };
                panel.OnClick += delegate
                {
                    bool shift = false;
                    Keys[] pressedKeys = Main.keyState.GetPressedKeys();
                    for (int i = 0; i < pressedKeys.Length; i++)
                    {
                        if (pressedKeys[i] == Keys.LeftShift || pressedKeys[i] == Keys.RightShift)
                        {
                            shift = true;
                        }
                    }

                    bool all = false;

                    if (shift)
                    {
                        all = true;
                    }

                    bool success = LansUncraftItems.instance.UncraftItem(this.item, recipe, all);

                    Recipe.FindRecipes();

                    if (!success)
                    {
                        Main.NewText(
                            "Not enough items in stack for this uncraft recipe.",
                            new Color(255, 0, 0));
                    }
                    else
                        Main.PlaySound(SoundID.Grab);
                    LansUncraftItems.instance.CloseRecipeGUI(success);
                };
                panel.Append(btn);
                recipeList.Add(panel);
            }
        }
    }

    internal sealed class UIRecipePanel : UIPanel
    {
        private readonly string tooltip;

        public UIRecipePanel(Recipe recipe)
        {
            foreach (Item item in recipe.requiredItem)
            {
                if (item.IsAir)
                    continue;

                tooltip += $"{item.HoverName} {System.Environment.NewLine}";
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            if (IsMouseHovering)
                Main.hoverItemName = tooltip;
        }
    }
}
