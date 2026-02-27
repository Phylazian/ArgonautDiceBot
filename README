# ArgonautDiceBot

ArgonautDiceBot is a free, community-driven tabletop session bot built for Valour.gg.

This project is licensed under Creative Commons Attribution-NonCommercial 4.0 International.
It is intended to remain free for community use and may not be used for commercial purposes.
Keep in mind this is v0.1-alpha. Expect bugs. Feedback welcome.

---

## Purpose

ArgonautDiceBot was built to provide a lightweight, secure tabletop utility for Valour planets.
Currently GURPS and D&D

Design goals:

- Respect planet autonomy
- Operate only when invoked by command
- Avoid disruptive automation
- Avoid data collection
- Remain free and community-accessible

---

## Features (v0.1)

- Multi-planet support
- Secure dice rolling
- Session start/end management
- DM-controlled locking
- Character registration (in-memory)
- Initiative tracking
- Ruleset switching (DND / GURPS)
- Per-channel session isolation

---

## Roles & Permissions

- This bot does not require pre-created DM or Player roles.
- The user who starts a session becomes the DM for that channel session.

---

## Data & Privacy

ArgonautDiceBot does not collect or store personal user data.

Current version (v0.1):

- No database
- No persistent storage
- No external API calls
- No background logging of messages
- All session data is stored in memory only
- All data clears when the bot restarts

---

## Security Model

- Bot token is loaded from environment variables only
- No hardcoded credentials
- No automated role modification
- No unsolicited DMs
- Commands must be invoked manually
- No message scraping

---

## License

This project is licensed under:

Creative Commons Attribution-NonCommercial 4.0 International (CC BY-NC 4.0)

You are free to:

- Use the bot
- Modify the bot
- Host the bot privately or publicly (free of charge)
- Contribute improvements

You may NOT:

- Sell this bot
- Charge for hosting access
- Include it in a paid product or service

Full license text available in the LICENSE file.

---

## Setup Instructions

### 1. Install .NET 8

Download from:
https://dotnet.microsoft.com/

### 2. Clone the Repository

git clone https://github.com/Phylazian/ArgonautDiceBot.git
cd ArgonautDiceBot

### 3. Set Bot Token

Windows (PowerShell):
setx VALOUR_BOT_TOKEN "your_token_here"

Linux/macOS:
export VALOUR_BOT_TOKEN=your_token_here

Restart terminal after setting environment variable.

### 4. Run the Bot

dotnet run

---

## Roadmap

Planned future improvements:

- SQLite persistence
- Improved permission system
- Advanced dice parsing
- Modular command handling
- Docker deployment support

---

## Maintainer

Created by John Shorette  
Community feedback is welcome.
