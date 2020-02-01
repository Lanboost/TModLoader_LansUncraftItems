using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using static Terraria.ModLoader.ModContent;

namespace LansUncraftItems
{
	public class RecipeBlockCondition
	{
		public virtual bool recipeCanBeUsed()
		{
			return false;
		}
	}

	public class RecipeBlock
	{
		public Recipe recipe;

		public RecipeBlockCondition condition;

		public RecipeBlock(Recipe recipe)
		{
			this.recipe = recipe;
		}
	}

	public enum UncraftResult : byte
	{
		Success,
		Full,
		NotEnoughItems
	}

	public class LansUncraftItems : Mod
	{
		PlayerInventoryWrapper inventoryWrapper = new PlayerInventoryWrapper();

		public static LansUncraftItems instance;

		internal Panel uncraftPanel;
		public UserInterface uncraftPanelInterface;
		internal RecipeGUI recipeGUI;
		public UserInterface recipeUI;

		Random random = new Random();

		public List<RecipeBlock> blockedRecipes = new List<RecipeBlock>();

		public LansUncraftItems()
		{
			instance = this;
		}

		public override void Load()
		{
			// this makes sure that the UI doesn't get opened on the server
			// the server can't see UI, can it? it's just a command prompt
			if (!Main.dedServ)
			{
				uncraftPanel = new Panel();
				uncraftPanel.Initialize();
				uncraftPanelInterface = new UserInterface();
				uncraftPanelInterface.SetState(uncraftPanel);

				recipeGUI = new RecipeGUI();
				recipeGUI.Initialize();
				recipeGUI.Activate();
				recipeUI = new UserInterface();
			}
		}

		public override void Unload()
		{
			instance = null;
		}

		public override void PostAddRecipes()
		{
			base.PostAddRecipes();

			Mod tokenMod = ModLoader.GetMod("TokenMod");
			if (tokenMod != null)
			{
				ModItem[] blockedCreateItems = new ModItem[] {
					tokenMod.GetItem("BasicEssence"),
					tokenMod.GetItem("Tier1Essence"),
					tokenMod.GetItem("Tier2Essence"),
					tokenMod.GetItem("Tier3Essence"),
					tokenMod.GetItem("Tier4Essence"),
					tokenMod.GetItem("Tier5Essence"),
					tokenMod.GetItem("Tier6Essence"),
					tokenMod.GetItem("Tier7Essence")
				};

				for (int j = 0; j < blockedCreateItems.Length; j++) {
					if(blockedCreateItems[j] != null)
					{
						var item = blockedCreateItems[j].item;
						for (int i = 0; i < Main.recipe.Length; i++)
						{
							if (Main.recipe[i] != null)
							{
								if (Main.recipe[i].createItem.type == item.type)
								{
									blockedRecipes.Add(new RecipeBlock(Main.recipe[i]));
								}
							}
						}
					}
				}
			}
			
			Mod imkSushisMod = ModLoader.GetMod("imkSushisMod");
			if (imkSushisMod != null)
			{
				
				ModItem[] blockedCreateItems = new ModItem[] {
					imkSushisMod.GetItem("FishingHardmodeToken"),
					imkSushisMod.GetItem("FishingStartToken"),
					imkSushisMod.GetItem("LootGoblinsToken"),
					imkSushisMod.GetItem("LootHardmodeToken"),
					imkSushisMod.GetItem("LootMartiansToken"),
					imkSushisMod.GetItem("LootMechToken"),
					imkSushisMod.GetItem("LootPiratesToken"),
					imkSushisMod.GetItem("LootPlanteraToken"),
					imkSushisMod.GetItem("LootSkeletronToken"),
					imkSushisMod.GetItem("LootStartToken"),
					imkSushisMod.GetItem("SpacePurityHardmodeToken"),
					imkSushisMod.GetItem("SpacePurityStartToken"),
					imkSushisMod.GetItem("SurfaceDesertStartToken"),
					imkSushisMod.GetItem("SurfacePurityEocToken"),
					imkSushisMod.GetItem("SurfacePurityStartToken"),
					imkSushisMod.GetItem("SwapToken"),
					imkSushisMod.GetItem("TempleJunglePlanteraToken"),
					imkSushisMod.GetItem("UndergroundCorruptionEocToken"),
					imkSushisMod.GetItem("UndergroundCrimsonEocToken"),
					imkSushisMod.GetItem("UndergroundDungeonSkeletronToken"),
					imkSushisMod.GetItem("UndergroundJungleStartToken"),
					imkSushisMod.GetItem("UndergroundPurityStartToken"),
					imkSushisMod.GetItem("UndergroundSnowStartToken"),
					imkSushisMod.GetItem("UnderwaterOceanStartToken"),
					imkSushisMod.GetItem("UnderworldHellSkeletronToken")
				};

				for (int j = 0; j < blockedCreateItems.Length; j++)
				{
					if (blockedCreateItems[j] != null)
					{
						var item = blockedCreateItems[j].item;
						for (int i = 0; i < Main.recipe.Length; i++)
						{
							if (Main.recipe[i] != null)
							{
								if (Main.recipe[i].createItem.type == item.type)
								{
									blockedRecipes.Add(new RecipeBlock(Main.recipe[i]));
								}
							}
						}
					}
				}
			}
		}

		public override void UpdateUI(GameTime gameTime)
		{
			// it will only draw if the player is not on the main menu
			if (Main.playerInventory)
			{
				uncraftPanelInterface?.Update(gameTime);
				recipeUI?.Update(gameTime);
			}
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (mouseTextIndex != -1)
			{
				layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
					"UncraftPanelLayer", DrawSomethingUI, InterfaceScaleType.UI));
				layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
					"RecipeGUILayer", DrawSomethingUI, InterfaceScaleType.UI));
			}
		}

		private bool DrawSomethingUI()
		{
			// it will only draw if the player is not on the main menu
			if (Main.playerInventory)
			{
				uncraftPanelInterface.Draw(Main.spriteBatch, new GameTime());
				recipeUI.Draw(Main.spriteBatch, new GameTime());
			}

			return true;
		}

		private UncraftResult UncraftItem(Item item, Recipe recipe)
		{
			if(item.type == recipe.createItem.type && item.stack >= recipe.createItem.stack)
			{
				inventoryWrapper.StartBatch();
				
				//success = inventoryWrapper.RemoveItem(item.type, 1);
				foreach (var required in recipe.requiredItem)
				{
					var reqItem = required.DeepClone();
					var ratioBack = GetInstance<Config>().Ratio;
					int back = (int)(reqItem.stack * ratioBack);
					float chanceForLast = (reqItem.stack * ratioBack) - back;
					if (random.NextDouble() <= chanceForLast)
					{
						back++;
					}
					reqItem.stack = back;

					if (!inventoryWrapper.AddItem(reqItem))
					{
						inventoryWrapper.Restore();
						return UncraftResult.Full;
					}
				}

				if (item.stack > recipe.createItem.stack)
				{
					item.stack -= recipe.createItem.stack;
				}
				else
				{
					item.TurnToAir();
				}

				return UncraftResult.Success;
			}
			return UncraftResult.NotEnoughItems;
		}

		public UncraftResult UncraftItem(Item item, Recipe recipe, bool all) {
			if (all)
			{
				int count = 0;
				UncraftResult result = UncraftItem(item, recipe);
				while (result == UncraftResult.Success)
				{
					count++;
					result = UncraftItem(item, recipe);
				}
				if (count > 0)
					return UncraftResult.Success;
				else
					return result;
			}
			else
			{
				return UncraftItem(item, recipe);
			}
		}

		public void UncraftItem(Item item, bool all)
		{
			List<Recipe> foundRecipes = new List<Recipe>();
			List<Recipe> foundBlockedRecipes = new List<Recipe>();
			for (int i = 0; i < Main.recipe.Length; i++)
			{
				if (Main.recipe[i] != null)
				{
					if(Main.recipe[i].createItem.type == item.type)
					{
						var inBlocked = false;
						foreach(var b in blockedRecipes)
						{
							if (b.recipe == Main.recipe[i])
							{
								inBlocked = true;
							}
						}

						if (inBlocked)
						{
							foundBlockedRecipes.Add(Main.recipe[i]);
						}
						else
						{
							foundRecipes.Add(Main.recipe[i]);
						}
					}
				}
			}

			if (foundRecipes.Count == 1)
			{
				UncraftResult result = UncraftItem(item, foundRecipes[0], all);

				Recipe.FindRecipes();

				switch (result)
				{
					case UncraftResult.Success:
						break;
					case UncraftResult.NotEnoughItems:
						Main.NewText(
							"Not enough items in stack for this uncraft recipe.",
							new Color(255, 0, 0));
						break;
					case UncraftResult.Full:
						Main.NewText(
							"Not enough space in inventory to return ingredients.",
							new Color(255, 0, 0));
						break;
				}
			}
			else if (foundRecipes.Count > 1)
			{
				OpenRecipeGUI();
				recipeGUI.ListRecipes(item, foundRecipes);
			}
			else
			{
				if (foundBlockedRecipes.Count > 0)
				{
					Main.NewText("Uncraft recipe for this item has been disabled.", new Color(255, 0, 0));
				}
				else
				{
					Main.NewText("No uncraft recipe found for this item.", new Color(255, 0, 0));
				}
			}
		}

		public void OpenRecipeGUI() => recipeUI.SetState(recipeGUI);

		public void CloseRecipeGUI(bool success)
		{
			if (!success)
			{
				Main.LocalPlayer.QuickSpawnClonedItem(
					recipeGUI.item, recipeGUI.item.stack);
			}
			recipeUI.SetState(null);
		}
	}

	public class PlayerInventoryWrapper
	{
		Item[] backup = new Item[50];

		public void StartBatch()
		{
			for (int k = 0; k < 50; k++)
			{
				backup[k] = Main.LocalPlayer.inventory[k].DeepClone();
			}
		}

		public void Restore()
		{
			for (int k = 0; k < 50; k++)
			{
				Main.LocalPlayer.inventory[k] = backup[k];
			}
		}

		public bool RemoveItem(int type, int count = 1)
		{
			for (int k = 0; k < 50; k++)
			{
				if (Main.LocalPlayer.inventory[k].type == type)
				{
					if (Main.LocalPlayer.inventory[k].stack <= count)
					{
						count -= Main.LocalPlayer.inventory[k].stack;
						Main.LocalPlayer.inventory[k].TurnToAir();

					}
					else
					{
						Main.LocalPlayer.inventory[k].stack -= count;
						return true;
					}
				}
			}
			return false;
		}

		public bool RemoveItem(Item item)
		{
			return RemoveItem(item.type, item.stack);
		}

		public bool AddItem(Item item)
		{
			for (int k = 0; k < 50; k++)
			{
				if (!Main.LocalPlayer.inventory[k].IsAir && Main.LocalPlayer.inventory[k].active && Main.LocalPlayer.inventory[k].type == item.type)
				{
					if (Main.LocalPlayer.inventory[k].stack+item.stack <= item.maxStack)
					{
						Main.LocalPlayer.inventory[k].stack += item.stack;
						return true;
					}
					else
					{
						int diff = item.maxStack - Main.LocalPlayer.inventory[k].stack;
						Main.LocalPlayer.inventory[k].stack = item.maxStack;
						item.stack -= diff;
					}
				}
			}

			for (int k = 0; k < 50; k++)
			{
				if (Main.LocalPlayer.inventory[k].IsAir)
				{
					Main.LocalPlayer.inventory[k] = item;
					return true;
				}
			}

			return false;
		}
	}
}