from __future__ import annotations

import re
import unicodedata


STOPWORDS = {
    "a",
    "an",
    "and",
    "as",
    "by",
    "do",
    "dla",
    "from",
    "i",
    "icon",
    "ikona",
    "ikon",
    "ikone",
    "ikonek",
    "ikonka",
    "ikonke",
    "in",
    "na",
    "of",
    "opis",
    "or",
    "png",
    "the",
    "to",
    "w",
    "with",
    "z",
}

TRANSLATION = str.maketrans(
    {
        "ą": "a",
        "ć": "c",
        "ę": "e",
        "ł": "l",
        "ń": "n",
        "ó": "o",
        "ś": "s",
        "ź": "z",
        "ż": "z",
        "Ą": "A",
        "Ć": "C",
        "Ę": "E",
        "Ł": "L",
        "Ń": "N",
        "Ó": "O",
        "Ś": "S",
        "Ź": "Z",
        "Ż": "Z",
        "С": "C",
        "с": "c",
    }
)

TOKEN_RE = re.compile(r"[a-z0-9]+")


ALIASES = {
    "air": ["aero", "aeromancer", "air", "powietrze", "powietrzny", "wiatr", "wietrzny", "wind"],
    "alchemy": ["alchemia", "alchemiczny", "alchemy"],
    "amulet": ["amulet", "amuletu", "naszyjnik"],
    "armor": ["armor", "armour", "cuirass", "pancerz", "zbroja", "zbroi"],
    "arrow": ["arrow", "arrows", "strzala", "strzaly"],
    "avatar": ["avatar", "avatars", "portret", "twarz"],
    "axe": ["axe", "axes", "topor", "topora", "topory"],
    "barbarian": ["barbarian", "barbarzynca", "berserk", "berserker"],
    "belt": ["belt", "belts", "pas", "pasek"],
    "blood": ["blood", "bloody", "krew", "krwawy", "krwi"],
    "bone": ["bone", "bones", "czaszka", "kosci", "kosc", "skull", "skulls"],
    "book": ["book", "books", "ksiega", "ksiegi", "ksiazka", "tome"],
    "boots": ["boots", "buty", "sabatons"],
    "bow": ["bow", "bows", "luk", "luku"],
    "bracers": ["bracer", "bracers", "brasers", "karwasze"],
    "buff": ["aura", "blessing", "buff", "boost", "bonus", "wzmocnienie"],
    "chaos": ["chaos", "chaotic", "spaczenie"],
    "chest": ["chest", "chests", "skarb", "skrzynia", "skrzynie", "treasure"],
    "craft": ["craft", "crafting", "material", "materials", "resource", "surowiec", "tworzenie"],
    "crossbow": ["crossbow", "kusza", "kusznik"],
    "curse": ["curse", "cursed", "klatwa", "przeklety"],
    "dagger": ["dagger", "daggers", "noz", "sztylet", "sztylety"],
    "dark": ["cien", "ciemny", "dark", "shadow", "void", "mrok", "mroczny"],
    "debuff": ["anti", "antibuff", "debuff", "oslabienie"],
    "displacement": [
        "displaced",
        "displacement",
        "force",
        "forced",
        "knockback",
        "odepchniecie",
        "odepchniety",
        "odpychanie",
        "przesuniecie",
        "przesuniety",
        "push",
        "pushed",
        "shift",
    ],
    "demon": ["demon", "demons", "diabel", "diabelski"],
    "druid": ["druid", "druidic", "druidyczny"],
    "dwarf": ["dwarf", "dwarven", "krasnolud"],
    "elf": ["elf", "elves", "elven", "elfi"],
    "engineering": ["engineer", "engineering", "inzynier", "mechaniczny"],
    "fairy": ["fairies", "fairy", "fae", "wrozka"],
    "farming": ["farm", "farming", "farma", "rolnictwo"],
    "fire": ["burn", "fire", "flame", "flames", "ogien", "ognia", "ognisty", "plomien", "plomienie", "pyro"],
    "fish": ["fish", "fishing", "ryba", "wedka"],
    "food": ["food", "jedzenie", "zarcie"],
    "gem": ["gem", "gems", "jewel", "klejnot", "krysztal"],
    "goblin": ["goblin", "goblins"],
    "heal": ["heal", "healing", "leczenie", "leczyc", "uzdrowienie"],
    "helmet": ["helm", "helmet", "helmets", "kask"],
    "holy": ["divine", "holy", "light", "sacred", "sanktuarium", "swiety"],
    "ice": ["cold", "cryo", "frost", "frozen", "ice", "lod", "lodowy", "mroz", "mrozu"],
    "jewelry": ["jewellery", "jewelry", "bizuteria"],
    "key": ["key", "keys", "klucz", "klucze"],
    "lightning": ["blyskawica", "electric", "elektryczny", "lightning", "piorun", "shock", "thunder"],
    "loot": ["drop", "loot", "lup", "nagroda"],
    "mace": ["buzdygan", "mace", "maces", "maczuga", "mlot"],
    "mage": ["caster", "czarodziej", "mage", "mag", "magic", "magiczny", "wizard"],
    "mana": ["energia", "mana", "many", "resource"],
    "meat": ["futro", "meat", "mieso", "skin", "skins", "skora"],
    "mining": ["mine", "mineral", "minerals", "mining", "ruda", "wydobycie"],
    "monster": ["creature", "mob", "monster", "monsters", "potwor", "wrog"],
    "mushroom": ["grzyb", "grzyby", "mushroom", "mushrooms"],
    "nature": ["berries", "berry", "druid", "fruit", "nature", "natura", "plant", "roslina", "warzywa"],
    "necromancer": ["necro", "necromancer", "necromancy", "nekromanta"],
    "paladin": ["knight", "paladin", "rycerz"],
    "paint": ["barwnik", "farba", "paint", "paints"],
    "pirate": ["pirat", "pirate"],
    "potion": ["eliksir", "mikstura", "potion", "potions"],
    "poison": ["jad", "poison", "toxin", "trucizna", "trujacy", "venom"],
    "priest": ["kaplan", "priest", "priestly"],
    "pull": ["pull", "pulled", "przyciagniecie", "przyciagniety"],
    "ranged": ["distance", "dystans", "ranged"],
    "ring": ["pierscien", "ring", "rings"],
    "rune": ["glyph", "glif", "runa", "rune", "runes"],
    "scroll": ["scroll", "scrolls", "zwój", "zwoj"],
    "shield": ["shield", "shields", "tarcza", "tarczy"],
    "sigil": ["sigil", "sigils", "symbol"],
    "skill": ["ability", "czar", "skill", "skills", "spell", "umiejetnosc"],
    "slow": ["slow", "spowolnienie", "spowolnic"],
    "spear": ["spear", "spears", "wlocznia", "wlocznie"],
    "speed": ["haste", "predkosc", "przyspieszenie", "speed", "szybkosc"],
    "staff": ["kostur", "laska", "staff", "staffs"],
    "summon": ["przywolanie", "summon", "summoner", "summoning"],
    "sword": ["blade", "miecz", "miecza", "sword", "swords"],
    "thief": ["assassin", "rogue", "thief", "zlodziej"],
    "transparent": ["przezroczyste", "transparent", "transperent"],
    "trousers": ["pants", "spodnie", "trousers"],
    "undead": ["nieumarly", "undead", "zombie"],
    "warlock": ["czarnoksieznik", "warlock"],
    "weapon": ["bron", "equipment", "gear", "item", "weapon", "weapons"],
}

CATEGORY_RULES = [
    (["aeromancer"], ["air", "wind", "mage", "skill"]),
    (["alchemy"], ["alchemy", "potion", "craft", "poison", "mana"]),
    (["anti", "buff"], ["debuff", "curse", "displacement", "slow", "skill"]),
    (["archer"], ["archer", "bow", "arrow", "ranged", "skill"]),
    (["arrow"], ["arrow", "displacement", "knockback", "movement", "push", "ranged", "weapon"]),
    (["artefact", "artifact"], ["artefact", "magic", "loot"]),
    (["axe"], ["axe", "melee", "weapon"]),
    (["barbarian"], ["barbarian", "melee", "skill"]),
    (["belt"], ["belt", "armor", "equipment"]),
    (["berries", "nuts"], ["food", "nature"]),
    (["blacksmith"], ["craft", "weapon", "armor"]),
    (["blood"], ["blood", "dark", "mage", "skill"]),
    (["bones", "sculls", "skulls"], ["bone", "undead", "loot"]),
    (["book"], ["book", "magic", "scroll"]),
    (["bow"], ["bow", "ranged", "weapon"]),
    (["braser", "bracer"], ["bracers", "armor", "equipment"]),
    (["buff"], ["buff", "speed", "skill"]),
    (["chaos"], ["chaos", "dark", "monster"]),
    (["chests", "keys", "treasure"], ["chest", "key", "treasure", "loot"]),
    (["civilian", "avatar"], ["avatar", "civilian"]),
    (["craft"], ["craft", "resource", "loot"]),
    (["cryo"], ["ice", "slow", "mage", "skill"]),
    (["cuirass"], ["armor", "equipment"]),
    (["curse"], ["curse", "debuff", "poison", "dark", "skill"]),
    (["dagger"], ["dagger", "melee", "weapon"]),
    (["dark", "elves"], ["dark", "elf", "avatar"]),
    (["demon"], ["demon", "dark", "monster"]),
    (["drop"], ["loot", "resource"]),
    (["druid"], ["druid", "nature", "skill"]),
    (["dwarf"], ["dwarf", "avatar", "loot"]),
    (["elf", "elves"], ["elf", "avatar"]),
    (["engineering"], ["engineering", "craft", "skill"]),
    (["exotic", "weapons"], ["weapon", "loot"]),
    (["fair"], ["fairy", "magic", "nature"]),
    (["farming"], ["farming", "food", "nature"]),
    (["fishing"], ["fish", "food"]),
    (["food"], ["food"]),
    (["fruit", "vegetable"], ["food", "nature"]),
    (["gem"], ["gem", "jewelry", "loot"]),
    (["goblin"], ["goblin", "monster", "loot"]),
    (["halloween", "helloween"], ["dark", "undead", "monster"]),
    (["helmet"], ["helmet", "armor", "equipment"]),
    (["lightning"], ["lightning", "mage", "skill"]),
    (["mace"], ["mace", "melee", "weapon"]),
    (["meat", "skin"], ["meat", "loot"]),
    (["mineral", "mining"], ["mining", "resource", "craft"]),
    (["mushroom"], ["mushroom", "poison", "nature", "alchemy"]),
    (["necro"], ["necromancer", "undead", "dark", "skill"]),
    (["paint"], ["paint", "craft"]),
    (["paladin"], ["paladin", "holy", "shield", "skill"]),
    (["pirate"], ["pirate", "weapon", "skill"]),
    (["potion"], ["potion", "heal", "mana", "poison", "alchemy"]),
    (["priest"], ["priest", "holy", "heal", "skill"]),
    (["pyro"], ["fire", "mage", "skill"]),
    (["ring", "jewellery", "jewelry"], ["ring", "jewelry", "loot"]),
    (["rune"], ["rune", "magic"]),
    (["sabatons"], ["boots", "armor", "equipment"]),
    (["scroll"], ["scroll", "magic"]),
    (["shield"], ["shield", "armor", "equipment"]),
    (["sigil"], ["sigil", "magic"]),
    (["spear"], ["spear", "melee", "weapon"]),
    (["staff"], ["staff", "mage", "weapon"]),
    (["summoner"], ["summon", "mage", "skill"]),
    (["sword"], ["sword", "melee", "weapon"]),
    (["thief"], ["thief", "dagger", "skill"]),
    (["trouser"], ["trousers", "armor", "equipment"]),
    (["undead"], ["undead", "dark", "monster"]),
    (["warlock"], ["warlock", "dark", "mage", "skill"]),
    (["crossbow", "rossbow"], ["crossbow", "ranged", "weapon", "skill"]),
]


def normalize_text(value: str) -> str:
    value = value.translate(TRANSLATION)
    value = re.sub(r"(?<=[a-z])(?=[A-Z])", " ", value)
    value = value.replace("&", " and ").replace("+", " ")
    value = unicodedata.normalize("NFKD", value)
    value = value.encode("ascii", "ignore").decode("ascii")
    return value.lower()


ALIAS_LOOKUP = {
    normalize_text(alias): canonical
    for canonical, aliases in ALIASES.items()
    for alias in aliases
}


def tokenize(value: str) -> list[str]:
    normalized = normalize_text(value)
    return [token for token in TOKEN_RE.findall(normalized) if token not in STOPWORDS]


def expand_terms(tokens: list[str]) -> set[str]:
    expanded = set(tokens)
    for token in tokens:
        canonical = ALIAS_LOOKUP.get(token)
        if canonical:
            expanded.add(canonical)
            expanded.update(normalize_text(alias) for alias in ALIASES.get(canonical, []))
    return {term for term in expanded if term and term not in STOPWORDS}


def infer_tags(text: str, tokens: list[str]) -> list[str]:
    normalized_text = normalize_text(text)
    token_set = set(tokens)
    tags: set[str] = set()

    for token in tokens:
        canonical = ALIAS_LOOKUP.get(token)
        if canonical:
            tags.add(canonical)

    for needles, rule_tags in CATEGORY_RULES:
        for needle in needles:
            normalized_needle = normalize_text(needle)
            if normalized_needle in token_set or normalized_needle in normalized_text:
                tags.update(rule_tags)
                break

    return sorted(tags)
