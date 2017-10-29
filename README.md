# Adminthulhu

Since this repository has recieved a bit of public attention over the years, it might be time for a README.

 -- INTRODUCTION --
 
Adminthulhu is an automated Discord bot, originally created for use only in the Monster Mash semi-public Discord server. Due to this, the complexity of the bot could be much higher and functionality less specialized. However, due to popular demand, a configuration system was implemented which now allows anyone to host their own customized version of the bot. Additionally, the bot is fully open source, and at least 15% of the code is quite easy to expand upon, allowing users to expand it with their own advanced functionality.

 -- GETTING IT RUNNING --
 
Getting this bot running on your own server requires, as previously mentioned, self-hosting. The bot is written in C# using the .NET Core multiplatform framework, and is therefore able to run on most platforms, however the automated patcher currently only works on Windows and Linux based systems.

Guide to installing the bot without patcher: https://pastebin.com/xhLinqRD

To download and use the automated patcher, as well as get support, visit the hub Discord server: https://discordapp.com/invite/ktm8DsD

 -- KEY FEATURES --
 
 Voice Channel Management
  - Automatically adds and removes channels on demand.
  - Automatically renames channels to reflect the games played within.
  - Automatically adds tags/icons to channel names to reflect their state.
  
 User Activity Monitor
  - Keeps track of three seperate "Activity States" for users, and highlights the most active users.
  - New members are automatically kicked if they go inactive within thier first two weeks.
  - Sounds incredibly shady.
  
 Automated Weekly Events
  - Creates and maintains a "voting board" using reactions as a voting menu.
  - At a set time of the week, these votes are counted and an event planned.
  - This process cycles each week.

 Robust Configuration System
  - JSON based configuration system which is easy for users to modify.
  - The code-side of this is incredibly easy and intuitive to use, allowing easy custom versions.
  
 Commands
  - A large selection of commands to use and abuse.
  - Using commands as arguments for commands in "Command-chains".
  - Commands to save and get variables from three different scopes.
  
 -- MINOR FEATURES --
 
  - Logs all activity on the server, including its own.
  - Sends a variety of messages to members after certain events, such as player joins.
  - Allows members access to certain gathered information, such as all played games ordered in "Books".
  - Keeps track of per-user data and settings, including their birthday if so desired. This allows the bot to wish people happy birthday! :D
  - Various kinds of customized message types handled by the bot, including "Book Messages" and the currently WIP "Poll Messages"
  - Automatic responses to certain phrases by certain users in certain channels by certain chance, or any combination of that.
  - By legal requirements, all End User Data is encrypted with a cheap-ass custom Ceaser encrypted, which code is in this repository!
  - COLOURS!
  
There is likely much not mentioned here, but these are at least the most used features.
  
 -- PLANS --
 
 Currently internally discussed (Read: I'm thinking about it) plans include some sort of API, which would allow other bots to control this bot, such as creating events at more specialized times. Additionally, I've been pondering on some sort of Adminthulhu network, which would be an opt-in system where all running Adminthulhu instances would be able to communicate. No idea what this could be used to do, but it might be fun to implement!

 -- LICENSE --
 
 The MIT License (MIT)

Copyright (c) 2016-2017 Marcus L. Jensen

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

This licensing is subject to change in future versions, as more fitting licenses might be found at a later time.

That's the first time I've ever put a license on something, and I have no idea if I did it right. If I did something wrong, please let me know!
