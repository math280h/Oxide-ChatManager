# Oxide-ChatManager

Simple chat manager for [Oxide](https://umod.org/).

## Features
* Word blacklist
* Ban/Unban from chat
* Karma

## Permissions
* `ChatManager.stats`
  * Gives access to `/cm.stats {Player name or ID}` command that shows user stats
* `ChatManager.ban`
  * Gives access to `/cm.ban {Player name or ID}` and `/cm.unban {Player name or ID}` command that bans/unbans the user from the chat
* `ChatManager.karma`
  * Gives access to `/cm.karma.reset {Player name or ID}` command that sets the player's karma to `0`

## Configuration
* `BlockMessage`
  *  Message to display when blocking a player message
* `BlockMessageColor`
  * The color of the `BlockMessage`
* `BlockedWords`
  * An array of words to block. If none leave  empty array
* `WhitelistedURLS`
  * An array of whitelisted URL's that the mod won't block.
* `Karma`
  * `minimum`
    * The minimum allowed karma before a user is banned from chat.
  * `WordWeight`
    * An object with words as keys and the value as the words karma weight
    ``"WordWeight": {"gg":5, "bg": -5}`` would result in players gaining 5 karma for saying `gg` and loosing 5 karma for saying `bg`