public class Constant
{
    public const int DEFAULT_LAYER = 0;
    public const int IGNORE_LAYER = 2;
    public const int PLAYER_LAYER = 6;
    public const int INTERACTABLE_LAYER = 3;
    // public const int NOT_RENDER_LAYER = 11;
    // public const int OUTLINE_LAYER = 12;

    public const string PLAYER_TAG = "Player";
    public const string GAME_SEAT_TAG = "GameSeat";
    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_GAME_MODE_DATA = "GameData";
    public const string KEY_PLAYER_ICON_ID = "PlayerIconID";
    public const string KEY_START_GAME = "StartGame";
    public const string KEY_HOST_ID = "HostID";
    public const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";
    public const string KEY_LOBBY_CODE = "LobbyCode";
    public const int MAX_PLAYERS = 8;
    public const float RESTART_INTERVAL = 5f;
    public const float START_GAME_COUNTDOWN = 5f;

  
    public const float DEFAULT_TURN_INTERVAL = 20f;
    public const float DEFAULT_VOTING_INTERVAL = 15f;
    public const float DEFAULT_DISCUSSION_INTERVAL = 40f;

    public const string DOG_ROLE_DESCRIPTION = "You are a <color=#00FF00>Loyal Dog</color>.\nWork with your pack to reach the gold card before the deck runs out to claim victory.";
    public const string CAT_ROLE_DESCRIPTION = "You are a <color=#FF0000>Crafty Cat</color>.\nPrevent the dogs from reaching the treasure until the deck is empty to win the round for the feline team.";

    public const string CLASSIC_MODE = "CLASSIC";
    public const string DAY_NIGHT_MODE = "DAY-NIGHT";
    public const string CLASSIC_MODE_DESCRIPTION = "Play the authentic <color=#ffff00>Saboteur</color> experience where <color=#00FF00>Loyal Dog</color> and <color=#FF0000>Crafty Cat</color> clash in a straightforward race for the treasure.\nFocus on <color=#00dd00>path-building</color> and <color=#dd0000>tactical sabotage</color>.";
    public const string DAY_NIGHT_DESCRIPTION = "A high-stakes <color=#cccccc>hybrid</color> of <color=#dddd00>Saboteur</color> and <color=#cc0000>Werewolf</color> where your moves are hidden by the cover of night.\nPlay your cards in secret during the <color=#00aaff>Night</color> phase, then debate, deflect, and vote to suspend suspicious players during the <color=#ffff00>Day</color> phase.";
}