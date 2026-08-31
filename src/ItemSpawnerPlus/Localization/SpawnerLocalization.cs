using System.Collections.Generic;

namespace ItemSpawnerPlus
{
    internal enum SpawnerText
    {
        Title,
        SearchPlaceholder,
        NoItems,
        NoMatches,
        Close,
        NotInRun,
        Loading,
        FilterTitle,
        FilterVanilla,
        FilterModded,
        FilterSpecial,
        FilterFood,
        FilterEquipment,
        FilterCreatures,
        CreatureBees,
        CreatureBeetle,
        CreatureBigGhost,
        CreatureScoutmasterMyres,
        CreatureZombie,
        CookTitle,
        CookUncooked,
        CookCooked,
        CookWellDone,
        CookBurnt,
        CookIncinerated,
    }

    internal static class SpawnerLocalization
    {
        // array order MUST match LocalizedText.Language: English, French, Italian, German,
        // SpanishSpain, SpanishLatam, BRPortuguese, Russian, Ukrainian, SimplifiedChinese,
        // TraditionalChinese, Japanese, Korean, Polish, Turkish
        private static readonly Dictionary<SpawnerText, string[]> _table = new Dictionary<SpawnerText, string[]>
        {
            [SpawnerText.Title] = new[]
            {
                "Item Spawner Plus", "Item Spawner Plus", "Item Spawner Plus", "Item Spawner Plus",
                "Item Spawner Plus", "Item Spawner Plus", "Item Spawner Plus", "Item Spawner Plus",
                "Item Spawner Plus", "Item Spawner Plus", "Item Spawner Plus", "Item Spawner Plus",
                "Item Spawner Plus", "Item Spawner Plus", "Item Spawner Plus",
            },
            [SpawnerText.SearchPlaceholder] = new[]
            {
                "Search...", "Rechercher...", "Cerca...", "Suchen...", "Buscar...", "Buscar...",
                "Pesquisar...", "Поиск...", "Пошук...", "搜索...", "搜尋...", "検索...", "검색...",
                "Szukaj...", "Ara...",
            },
            [SpawnerText.NoItems] = new[]
            {
                "No items available.", "Aucun objet disponible.", "Nessun oggetto disponibile.",
                "Keine Gegenstände verfügbar.", "No hay objetos disponibles.", "No hay objetos disponibles.",
                "Nenhum item disponível.", "Нет доступных предметов.", "Немає доступних предметів.",
                "没有可用的物品。", "沒有可用的物品。", "利用可能なアイテムがありません。",
                "사용 가능한 아이템이 없습니다.", "Brak dostępnych przedmiotów.", "Kullanılabilir eşya yok.",
            },
            [SpawnerText.NoMatches] = new[]
            {
                "No items match your search.", "Aucun objet ne correspond.", "Nessun oggetto corrisponde.",
                "Keine Treffer für deine Suche.", "Ningún objeto coincide.", "Ningún objeto coincide.",
                "Nenhum item corresponde.", "Ничего не найдено.", "Нічого не знайдено.",
                "没有匹配的物品。", "沒有符合的物品。", "一致するアイテムがありません。",
                "일치하는 아이템이 없습니다.", "Brak pasujących przedmiotów.", "Aramanızla eşleşen eşya yok.",
            },
            [SpawnerText.Close] = new[]
            {
                "Close", "Fermer", "Chiudi", "Schließen", "Cerrar", "Cerrar", "Fechar",
                "Закрыть", "Закрити", "关闭", "關閉", "閉じる", "닫기", "Zamknij", "Kapat",
            },
            [SpawnerText.NotInRun] = new[]
            {
                "Start or join a run to spawn items.",
                "Lancez ou rejoignez une partie pour faire apparaître des objets.",
                "Avvia o unisciti a una partita per generare oggetti.",
                "Starte oder tritt einem Lauf bei, um Gegenstände zu erzeugen.",
                "Inicia o únete a una partida para generar objetos.",
                "Inicia o únete a una partida para generar objetos.",
                "Inicie ou entre em uma partida para gerar itens.",
                "Начните или присоединитесь к забегу, чтобы создавать предметы.",
                "Почніть або приєднайтеся до забігу, щоб створювати предмети.",
                "开始或加入一局游戏才能生成物品。",
                "開始或加入一場遊戲才能生成物品。",
                "アイテムを出すにはランを開始または参加してください。",
                "아이템을 생성하려면 런을 시작하거나 참가하세요.",
                "Rozpocznij lub dołącz do przebiegu, aby tworzyć przedmioty.",
                "Eşya üretmek için bir koşu başlatın veya bir koşuya katılın.",
            },
            [SpawnerText.Loading] = new[]
            {
                "Loading...", "Chargement...", "Caricamento...", "Wird geladen...",
                "Cargando...", "Cargando...", "Carregando...", "Загрузка...", "Завантаження...",
                "加载中...", "載入中...", "読み込み中...", "로딩 중...", "Wczytywanie...", "Yükleniyor...",
            },
            [SpawnerText.FilterTitle] = new[]
            {
                "Filter", "Filtre", "Filtro", "Filter", "Filtro", "Filtro", "Filtro",
                "Фильтр", "Фільтр", "筛选", "篩選", "フィルター", "필터", "Filtr", "Filtre",
            },
            [SpawnerText.FilterVanilla] = new[]
            {
                "Vanilla", "", "", "", "", "", "", "Ванильные", "Ванільні",
                "原版", "原版", "バニラ", "바닐라", "", "",
            },
            [SpawnerText.FilterModded] = new[]
            {
                "Modded", "", "", "", "", "", "", "Модовые", "Модові",
                "模组", "模組", "MOD", "모드", "", "",
            },
            [SpawnerText.FilterSpecial] = new[]
            {
                "Special", "Spécial", "Speciale", "Spezial", "Especial", "Especial", "Especial",
                "Особые", "Особливі", "特殊", "特殊", "特殊", "특수", "Specjalne", "Özel",
            },
            [SpawnerText.FilterFood] = new[]
            {
                "Food", "Nourriture", "Cibo", "Nahrung", "Comida", "Comida", "Comida",
                "Еда", "Їжа", "食物", "食物", "食べ物", "음식", "Jedzenie", "Yiyecek",
            },
            [SpawnerText.FilterEquipment] = new[]
            {
                "Equipment", "Équipement", "Equipaggiamento", "Ausrüstung", "Equipo", "Equipo", "Equipamento",
                "Снаряжение", "Спорядження", "装备", "裝備", "装備", "장비", "Ekwipunek", "Ekipman",
            },
            [SpawnerText.FilterCreatures] = new[]
            {
                "Creatures", "Créatures", "Creature", "Kreaturen", "Criaturas", "Criaturas", "Criaturas",
                "Существа", "Істоти", "生物", "生物", "クリーチャー", "생물", "Stwory", "Yaratıklar",
            },
            [SpawnerText.CreatureBees] = new[]
            {
                "Bees", "Abeilles", "Api", "Bienen", "Abejas", "Abejas", "Abelhas",
                "Пчёлы", "Бджоли", "蜜蜂", "蜜蜂", "ハチ", "벌", "Pszczoły", "Arılar",
            },
            [SpawnerText.CreatureBeetle] = new[]
            {
                "Beetle", "Scarabée", "Scarabeo", "Käfer", "Escarabajo", "Escarabajo", "Besouro",
                "Жук", "Жук", "甲虫", "甲蟲", "カブトムシ", "딱정벌레", "Żuk", "Böcek",
            },
            [SpawnerText.CreatureBigGhost] = new[]
            {
                "Big Ghost", "Grand Fantôme", "Fantasma gigante", "Großer Geist", "Fantasma grande",
                "Fantasma grande", "Fantasma grande", "Большой призрак", "Великий привид", "大幽灵",
                "大幽靈", "大きな幽霊", "큰 유령", "Wielki duch", "Büyük Hayalet",
            },
            // proper name of the boss / area, kept as-is in every language
            [SpawnerText.CreatureScoutmasterMyres] = new[]
            {
                "Scoutmaster Myres", "", "", "", "", "", "", "", "", "", "", "", "", "", "",
            },
            [SpawnerText.CreatureZombie] = new[]
            {
                "Zombie", "Zombie", "Zombie", "Zombie", "Zombi", "Zombi", "Zumbi",
                "Зомби", "Зомбі", "僵尸", "殭屍", "ゾンビ", "좀비", "Zombie", "Zombi",
            },
            [SpawnerText.CookTitle] = new[]
            {
                "Cook level", "Niveau de cuisson", "Livello di cottura", "Gargrad",
                "Nivel de cocción", "Nivel de cocción", "Nível de cozimento",
                "Уровень прожарки", "Рівень просмаження", "烹饪等级", "烹飪等級",
                "焼き加減", "익힘 정도", "Poziom wypieczenia", "Pişirme seviyesi",
            },
            [SpawnerText.CookUncooked] = new[]
            {
                "Uncooked", "Cru", "Crudo", "Roh", "Sin cocinar", "Sin cocinar", "Cru",
                "Сырое", "Сире", "未烹饪", "未烹飪", "生", "익히지 않음", "Surowe", "Çiğ",
            },
            [SpawnerText.CookCooked] = new[]
            {
                "Cooked", "Cuit", "Cotto", "Gegart", "Cocinado", "Cocinado", "Cozido",
                "Приготовлено", "Приготовано", "已烹饪", "已烹飪", "調理済み", "익힘", "Ugotowane", "Pişmiş",
            },
            [SpawnerText.CookWellDone] = new[]
            {
                "Well-Done", "Bien cuit", "Ben cotto", "Durchgebraten", "Muy hecho", "Muy hecho",
                "Bem passado", "Хорошо прожарено", "Добре просмажено", "全熟", "全熟",
                "よく焼き", "완전히 익힘", "Dobrze wysmażone", "İyi pişmiş",
            },
            [SpawnerText.CookBurnt] = new[]
            {
                "Burnt", "Brûlé", "Bruciato", "Verbrannt", "Quemado", "Quemado", "Queimado",
                "Подгорело", "Підгоріло", "烧焦", "燒焦", "焦げ", "탐", "Spalone", "Yanmış",
            },
            [SpawnerText.CookIncinerated] = new[]
            {
                "Incinerated", "Incinéré", "Incenerito", "Eingeäschert", "Incinerado", "Incinerado",
                "Incinerado", "Испепелено", "Спопеліло", "焚毁", "焚毀", "灰化", "재가 됨",
                "Spopielone", "Kül olmuş",
            },
        };

        public static string Get(SpawnerText key) => LocalizationHelper.Resolve(_table[key]);
    }
}
