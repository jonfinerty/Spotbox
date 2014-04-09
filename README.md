Spotbox
=======

A .NET Spotify Server.

Very early stages at the moment.

The goal is to create a windows-based spotify instance that can be controlled over the web via http calls.


# Installation

Rename `App.example.config` to `App.config` and replace spotify username, password and key location with your own account details.

In order to open the appropriate port, Spotbox will need to be run as adminstrator.

(Nuget packages should be restored automatically when the solution is built)

# API

Rough documentation

## Controls

Used for controlling the playing music

`POST /play` - Starts the current track playing. Returns a 404 if no track is set.

`POST /pause` - Pauses the current track.

`POST /next` - Skips to the next track in the current playlist. Playback state will be the same on the new track (paused if previous track was paused)

`POST /prev` - Skips to the previous track in the current playlist.

## Playing 

Used for getting the current track which is playing

`GET /playing` - Returns a JSON representation of the current track.

`GET /playing/cover.jpeg` - Returns a jpeg of the current track's album cover.

## Playlist

Used for getting, adding to, and changing the current playlist

`GET /playlist` - Returns a JSON representation of the current playlist

`POST /playlist` - Accepts JSON in the form `{"Value" : "track query string"}` searches for a track and adds it to the end of the current playlist

`PUT /playlist` - Accepts JSON in the form `{"Value" : "Playlist Name"}` searches for a playlist with a matching name and replaces the current playlist with it (setting the position to 0)

## Playlists

Used for getting a list of available playlists

`GET /playlists` - Returns a JSON representation of all the available playlists

## Speak

For a bit of fun, you can get Spotbox to talk

`POST /speak` - Accepts JSON in the form `{"Value" : "Text to read out"}` and pauses the audio, reads the text and restarts the audio

