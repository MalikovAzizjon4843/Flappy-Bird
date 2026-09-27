// O'yin holatlari. GameManager boshqaradi, UIManager har o'zgarishda panellarni almashtiradi.
public enum GameState
{
    Menu,     // bosh menyu, vaqt to'xtagan
    Ready,    // "Ekranga teging": qush joyida tebranadi, quvurlar yo'q; birinchi tap → Playing
    Playing,
    Paused,
    Dying,    // urilgandan keyin ~0.5 s: qush aylanib tushadi, dunyo to'xtagan, keyin GameOver paneli
    GameOver
}
