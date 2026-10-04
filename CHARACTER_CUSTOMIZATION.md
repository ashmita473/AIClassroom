# Character Customization

Implemented:
- Boy/Girl character base.
- Skin, hair, hair color, outfit, hat, accessory, shoes and pet categories.
- Each item has an XP cost and permanent unlock.
- New students start with 200 spendable XP.
- Lifetime earned XP remains separate from the spendable XP wallet.
- Cat pet costs 1000 XP.
- XP earned from lessons, games, quizzes and assignments increases both lifetime XP and spendable XP.
- Character purchases subtract spendable XP and create an XP transaction.
- Student dashboard includes a Customize Your Character entry point.
- Customizer provides a layered pixel-art preview and wardrobe.
- Default items are free; rare items cost more XP.
- The XP wallet is not automatically topped up on every application restart.

Database note:
The schema now has new character tables and XpBalance. Because the current app uses EnsureCreated during development, drop and recreate the development database once after installing this version so SQL Server creates the new columns/tables.
