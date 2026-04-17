/////////////////////////////////////////////////////////////////////////////////////////////////////
//
// Audiokinetic Wwise generated include file. Do not edit.
//
/////////////////////////////////////////////////////////////////////////////////////////////////////

#ifndef __WWISE_IDS_H__
#define __WWISE_IDS_H__

#include <AK/SoundEngine/Common/AkTypes.h>

namespace AK
{
    namespace EVENTS
    {
        static const AkUniqueID BOSS_SLIMEALIVE = 215348046U;
        static const AkUniqueID BOSS_SLIMEDEAD = 3430058625U;
        static const AkUniqueID BOSS_WISPALIVE = 3209282391U;
        static const AkUniqueID BOSS_WISPDEAD = 2298776174U;
        static const AkUniqueID CHEST_AMBIENT = 3464756077U;
        static const AkUniqueID CHEST_AMBIENT_STOP = 1316707488U;
        static const AkUniqueID CHEST_LOCKED = 895085565U;
        static const AkUniqueID CHEST_OPEN = 2728948375U;
        static const AkUniqueID ENEMY_LIGHTNING_DAMAGED = 608341856U;
        static const AkUniqueID ENEMY_SPAWN = 1526102535U;
        static const AkUniqueID GOLEM_HIT = 2529293365U;
        static const AkUniqueID GOLEM_THROW = 3954951306U;
        static const AkUniqueID PICK_UP_ITEM = 4229230304U;
        static const AkUniqueID PLAY_AMBIENCE = 278617630U;
        static const AkUniqueID PLAY_PLAYER_CROWN_SHOOT = 1245563929U;
        static const AkUniqueID PLAY_PLAYER_DASH = 2175711460U;
        static const AkUniqueID PLAY_PLAYER_EXPLOSION = 2417773581U;
        static const AkUniqueID PLAY_PLAYER_FIRE = 1408288908U;
        static const AkUniqueID PLAY_PLAYER_FIREBALL = 1959126323U;
        static const AkUniqueID PLAY_PLAYER_FIREBALLS = 109563706U;
        static const AkUniqueID PLAY_PLAYER_GROUND_SLAM = 1163424219U;
        static const AkUniqueID PLAY_PLAYER_JUMP = 562256996U;
        static const AkUniqueID PLAY_PLAYER_LIGHTNING_EXPLOSION = 3008676426U;
        static const AkUniqueID PLAY_PLAYER_MAJOR_LIGHTNING_ATTACK = 865153475U;
        static const AkUniqueID PLAY_PLAYER_MELEE = 3619611380U;
        static const AkUniqueID PLAY_PLAYER_ZAP = 1666518157U;
        static const AkUniqueID PLAY_RIVER1 = 2332603225U;
        static const AkUniqueID PLAY_RIVER2 = 2332603226U;
        static const AkUniqueID PLAY_SONG_BOSS = 2758397059U;
        static const AkUniqueID PLAY_SONG_CAVE = 332365527U;
        static const AkUniqueID PLAY_SONG_FOREST = 3919329323U;
        static const AkUniqueID PLAY_SONG_ICEY = 3808693238U;
        static const AkUniqueID PLAY_TELEPORTER_AMBIENCE = 1203559441U;
        static const AkUniqueID PLAY_TUTORIAL = 283731184U;
        static const AkUniqueID PLAY_UI_BUTTON_CLICK = 1661558166U;
        static const AkUniqueID ROCK_BREAK = 756826226U;
        static const AkUniqueID SKULL_DEATH = 2252982343U;
        static const AkUniqueID SLIME_BOSS_DAMAGE = 804704067U;
        static const AkUniqueID SLIME_BOSS_JUMP = 1864203916U;
        static const AkUniqueID SLIME_DEATH = 510073564U;
        static const AkUniqueID SLIME_JUMP = 2006797914U;
        static const AkUniqueID STOP_ALL = 452547817U;
        static const AkUniqueID WISP_SHOOT = 1320046920U;
    } // namespace EVENTS

    namespace STATES
    {
        namespace BOSS_ALIVE
        {
            static const AkUniqueID GROUP = 4265060404U;

            namespace STATE
            {
                static const AkUniqueID ALIVE = 655265632U;
                static const AkUniqueID DEAD = 2044049779U;
                static const AkUniqueID NONE = 748895195U;
            } // namespace STATE
        } // namespace BOSS_ALIVE

        namespace MUSIC_STATES
        {
            static const AkUniqueID GROUP = 1690668539U;

            namespace STATE
            {
                static const AkUniqueID CAVE = 4122393694U;
                static const AkUniqueID FOREST = 491961918U;
                static const AkUniqueID ICEY = 1756006875U;
                static const AkUniqueID NONE = 748895195U;
            } // namespace STATE
        } // namespace MUSIC_STATES

    } // namespace STATES

    namespace SWITCHES
    {
        namespace SWITCH_MUSIC_REGION
        {
            static const AkUniqueID GROUP = 275596552U;

            namespace SWITCH
            {
                static const AkUniqueID CAVE = 4122393694U;
                static const AkUniqueID FOREST = 491961918U;
                static const AkUniqueID TREE = 3322072369U;
            } // namespace SWITCH
        } // namespace SWITCH_MUSIC_REGION

    } // namespace SWITCHES

    namespace GAME_PARAMETERS
    {
        static const AkUniqueID AMBIENTVOLUME = 3546521921U;
        static const AkUniqueID DISTANCETOEXTRACTIONPOINT = 3040750302U;
        static const AkUniqueID ENEMYVOLUME = 535618029U;
        static const AkUniqueID MASTERVOLUME = 2918011349U;
        static const AkUniqueID MUSICVOLUME = 2346531308U;
        static const AkUniqueID PLAYERVOLUME = 1399119200U;
    } // namespace GAME_PARAMETERS

    namespace BUSSES
    {
        static const AkUniqueID AMBIENT_BUS = 1207363161U;
        static const AkUniqueID ENEMY_BUS = 963934797U;
        static const AkUniqueID MAIN_AUDIO_BUS = 2246998526U;
        static const AkUniqueID MUSIC_BUS = 3127962312U;
        static const AkUniqueID PLAYER_BUS = 174537428U;
    } // namespace BUSSES

    namespace AUX_BUSSES
    {
        static const AkUniqueID CAVE_BUS = 1180036773U;
    } // namespace AUX_BUSSES

    namespace AUDIO_DEVICES
    {
        static const AkUniqueID NO_OUTPUT = 2317455096U;
        static const AkUniqueID SYSTEM = 3859886410U;
    } // namespace AUDIO_DEVICES

}// namespace AK

#endif // __WWISE_IDS_H__
