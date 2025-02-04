# Text file Tools for the Elgato Stream Deck

A set of tools for manipulating text files through the Elgato Stream Deck. Useful for live stream updates

 **Author's website and contact information:** [https://barraider.com](https://barraider.com)

## New in v1.5
- Both `Next Line Writer` and `Random Line Writer` now support choosing from different types of outputs (Simulate typing, Copy to clipboard, Save to file)

## New in 1.4
- New `Next Line` action cycles through a text file and output the next line on every keypress
- Both `Next Line` and `Random Line` actions now support outputting to clipboard
- New `Regex Display` action parses a text file for a regex and displays the match on key.
- Both `Regex Display` and `Last Word Display` support Auto Stopping the Flashing Alert when the text no longer matches.

## Current Features
* Text File Updater - Overwrites the contents of a text file with a predefined string. Use to change overlay text during Stream
* Last Word Display - Shows the last word of a text file on your Stream Deck
    - Can alert if the text equals a preset value. This works great with the `CountDown Timer` if you're saving the timer to a file and want to show it/alert on it from a different profile.
* Random Line Writer - Sends a random line from a text file to your keyboard, useful for giveaways or just sending a random 'hello' message in chat.
* Next Line - cycles through a text file and output the next line on every keypress
- Regex Display - parses a text file for a regex and displays the match on key.
- Both `Regex Display` and `Last Word Display` support Auto Stopping the Flashing Alert when the text no longer matches.
- Both `Next Line` and `Random Line` actions now support outputting to clipboard

### Download

* [Download plugin](https://github.com/BarRaider/streamdeck-textfiletools/releases/)

## I found a bug, who do I contact?
For support please contact the developer. Contact information is available at https://barraider.com

## I have a feature request, who do I contact?
Please contact the developer. Contact information is available at https://barraider.com

## Dependencies
* Uses StreamDeck-Tools by BarRaider: [![NuGet](https://img.shields.io/nuget/v/streamdeck-tools.svg?style=flat)](https://www.nuget.org/packages/streamdeck-tools)
* Uses [Easy-PI](https://github.com/BarRaider/streamdeck-easypi) by BarRaider - Provides seamless integration with the Stream Deck PI (Property Inspector) 

## Change Log

## New in v1.3
- `Last Word Display` action now supports splitting long words into multiple lines on the key.
- `Text File Updater` action now supports appending data (instead of only overwriting like before).
- Updated input textbox in `Text File Updater` to support multiple lines of text


## New in v1.2
- `Last Word Display` action can now alert if the text equals a preset value. This works great with the `CountDown Timer` if you're saving the timer to a file and want to show it/alert on it from a different profile.
- `Last Word Display` now supports modifying the Title settings from the Title properties menu
- Added Multi-Action support to all actions


## New in v1.1.5
- Random Line action now supports `Sending Enter` key at the end of the line. Useful if using for things like Chat, and you want the random line to be sent automatically.
