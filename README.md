# Adminthulhu

Since this repository has recieved a bit of public attention over the years, it might be time for a README.

 -- INTRODUCTION --
 
Adminthulhu is an automated Discord bot, created for use in the Monster Mash semi-public Discord server. Due to this, the complexity of the bot can be much higher, with a lot more API calls avaialble at any time. At the same time, it is also much easier to program, since there are no checks or data neccesary for different servers. However, because of this, it of course cannot be run on any other server without heavy modification.

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
  
 Commands
  - A large selection of commands to use and abuse.
  - A clean, easy to expand command framework.
  - Refusal to send most command responses in main channel.
  
 -- MINOR FEATURES --
 
  - Logs all activity on the server, including its own.
  - Sends a variety of messages to members after certain events, such as player joins.
  - Allows members access to certain gathered information, such as top 20 most played games.
  - Keeps track of per-user data and settings, including their birthday if so desired. This allows the bot to wish people happy birthday! :D
  - Automatic responses to certain phrases by certain users in certain channels by certain chance, or any combination of that.
  - COLOURS!
  
There is likely much not mentioned here, but these are at least the most used features.
  
 -- PLANS --
 
Currently there are no feature-related plans, and most work on the bot is towards smaller details, as well as bug fixes. One big featured that'd be amazing would be a centralized configuration manager, which in turn would allow anyone to configure their own variant of the bot quite easily, in order to allow this same code to run on multiple servers. Only issue with this is the amount of in-jokes and server specific features implemented.

 -- LICENSE --
 
 The MIT License (MIT)

Copyright (c) 2016-2017 Marcus L. Jensen

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

This licensing is subject to change in future versions, as more fitting licenses might be found at a later time.

That's the first time I've ever put a license on something, and I have no idea if I did it right. If I did something wrong, please let me know!
