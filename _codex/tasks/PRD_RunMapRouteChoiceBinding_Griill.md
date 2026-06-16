# [TARENA] PRD: Route Choice to Path Map Binding (020->021)

- Status: draft-ready-for-grill
- Type: HITL Task
- Area: Run Metagame, Start Run, Run Map, Route Preview
- Label: ready-for-agent
- Parent: `_codex/tasks/019_PRD_RunMetagameRewardFramework.md` and `_codex/tasks/021_PRD019_RunMap.md`
- Blocked by: `_codex/tasks/020_PRD019_StartRun.md`

## Problem Statement

Gracz wybiera trasę na ekranie 020 (Start Run), ale ten wybór nie zawsze ma
wyraźny, odczuwalny wpływ na faktyczny układ mapy w 021 (Run Map). Obecnie preview
prezentuje „route”, natomiast 021 pokazuje „paths”, więc po stronie gracza powstaje
wrażenie, że decyzja była głównie kosmetyczna. Potrzebny jest spójny model, w
którym wybór route ma jednoznaczny skutek gameplayowy i jest czytelny jeszcze przed
startem runu.

## Solution

Wprowadzić wyraźny poziom pośredni:  
`RouteChoice` (020) -> `PathProfile` -> `PathDefinition` (`path-*`) -> `NodeDefinition` (022/021).  

W praktyce oznacza to:

- Preview w 020 pokazuje skrótowy „signature” route (skład typów node’ów, ryzyko,
  bias nagród), nie pełną mapę.
- Start Run przekazuje `RouteChoiceId` do Run Map creation.
- Run Map tworzy/ładuje zbiór ścieżek spójny z wybranym route.
- W 021 gracz gra na mapie, której układ i węzły wynikają z podjętej wcześniej
  decyzji.

## User Stories

1. Jako gracz, chcę wybrać jedną z opcji Route w 020, aby od razu wiedzieć, jaki
   typ doświadczenia czeka mnie w 021, żebym mógł podjąć świadomą decyzję.
2. Jako gracz, chcę zobaczyć w 020 skrótowy podpis trasy (np. „2x Battle, 1x Shop,
   Final Boss”), aby szybko ocenić profil ryzyka bez czytania pełnej mapy.
3. Jako gracz, chcę widzieć „recommended value” względem mojej aktualnej armii,
   aby łatwo porównać, czy dana ścieżka pasuje do mojego power-level.
4. Jako gracz, chcę, by wybrany RouteChoice miał wpływ na to, czy droga startowa jest
   bardziej ryzykowna czy bardziej bezpieczna, żebym widział różnicę między opcjami.
5. Jako gracz, chcę, by 020 ograniczało do trzech czytelnych route’ów V1, a nie
   pokazywało dowolnego chaosu opcji, aby decyzja była szybka i jednoznaczna.
6. Jako gracz, chcę po kliknięciu Begin Run natychmiast wejść do mapy, która
   odzwierciedla mój wybór, aby nie czuć „fałszywego wyboru”.
7. Jako gracz, chcę mieć przewidywalne „Possible Reward hint” i „Expected Risk”,
   aby podejmować decyzje przy niepełnej informacji.
8. Jako gracz, chcę widzieć etapowanie mapy (stage progress), aby wiedzieć gdzie jestem.
9. Jako gracz, chcę mieć lock dla Final Boss zgodnie z osiągniętym postępem trasy,
   aby stopniowo otwierać trudniejsze fragmenty.
10. Jako gracz, chcę, by wybór ścieżki był deterministyczny dla danego id sesji,
    żebym po re-open nie tracił spójności runa.
11. Jako gracz, chcę, by ponowne wejście do 021 po wejściu/wyjściu zachowywało
    postęp (current node, completed nodes, gold), żeby gra nie resetowała się.
12. Jako gracz, chcę, by „Route preview nodes” w 020 były jedynie symbolem,
    a nie klikanym interaktywnym planem mapy, żeby UI nie myliło ról ekranów.
13. Jako designer, chcę zdefiniować per-route warianty ścieżek, aby testy balansu
    i warianty trudności były szybkie.
14. Jako designer, chcę mieć prosty, rozszerzalny katalog route profiles, żeby dodać
    nowy route bez przebudowy UI 020.
15. Jako designer, chcę zobaczyć, że jedna opcja route nie może zawierać identycznego
    zestawu pathów bez uzasadnienia, żeby uniknąć duplicate contentu.
16. Jako developer, chcę mieć oddzielny, testowalny moduł mapowania RouteChoiceId
    do listy ścieżek, żeby uniknąć mieszania logiki UI i serwisu mapy.
17. Jako developer, chcę zachować bezpieczny kontrakt 020->021 (przekaz `RouteChoiceId`
    + `StartRunResult`), żeby przyszły Online adapter mógł przejąć źródło prawdy bez
    zmiany UI.
18. Jako developer, chcę, aby mapa w 021 miała jawny fallback, gdy wpis RouteChoiceId
    jest nieznany (np. domyślny route), żeby uniknąć crashy i zapewnić kontynuację gry.
19. Jako tester, chcę mieć testy jednostkowe pokrywające:
    - różne RouteChoice->mapy,
    - brak wyboru route,
    - fallback route,
    - deterministyczne seedowanie mapy,
    żebym mógł automatycznie wychwycić regresję.
20. Jako QA, chcę mieć metryki ręcznego playtestu (czytelność różnic route i odczuwalna
    konsekwencja), aby grillowanie decyzji było oparte na danych, nie odczuciu „wydaje się”.

## Implementation Decisions

- Wprowadzić koncept `RouteChoiceId` jako semantyczny klucz konfiguracji trasy.
- Wydzielić moduł `RouteChoiceBinding` (lub równoważny), którego odpowiedzialność:
  zmapowanie `RouteChoiceId` -> `RouteProfile`.
- `RouteProfile` obejmuje:
  - metadane prezentacji (nazwa, opis, bias),
  - parametry preview’u (skrótowe typy node’ów, poziom ryzyka, sugerowane nagrody),
  - identyfikatory zestawów ścieżek (`path-*`) dopuszczone dla danego wyboru.
- `RunMapService` pobiera profile mapy przez warstwę katalogu pathów:
  `BuildPaths(routeChoiceId)` zamiast statycznego listowania stałych ścieżek.
- `RunMapStateRecord` zachowuje `SelectedRouteChoiceId` jako źródło zgodności do dalszej
  serializacji i odtworzenia mapy.
- `StartRunCommand`/`StartRunResult` pozostają nośnikiem wyboru route przy starcie runa.
- `OfflineRunMapDbStore` zapisuje mapę opartą o `selectedRouteChoiceId` i generuje
  weryfikowalną strukturę node’ów/pathów.
- 020 UI:
  - pokazywać podgląd profilu route jako `signature` (typy node’ów, ryzyko, bias),
  - pokazywać stan „selected” dla wybranego route,
  - nie renderować tam pełnej mapy node’ów w trybie aktywnego wyboru ruchu.
- 021 UI:
  - czytać gotową listę pathów i node’ów,
  - traktować `Run Map` jako etap interakcyjny (travel/availability),
  - zachować różnicę między „route preview” a „route execution”.
- Nie zmieniamy floatów balansu ani parametrów jednostek podczas tego zadania.
- Nie zmieniamy nazw publicznych/serialized fieldów, nie ruszamy scen prefabów ani
  assetów bez osobnej zgody użytkownika.

Decyzja modelowa (schemat):

```text
RouteChoice
  id
  displayName
  previewSignature  // display-only
  recommendedProfile
  pathSelectionPolicy

RouteProfile
  pathTemplateId[]
  rewardBias
  riskBand

RouteMapState
  runId
  selectedRouteChoiceId
  paths: PathViewModel[]
  completedNodes[]
  currentNodeId
  stageProgress
```

## Testing Decisions

- Testy mają sprawdzać efekt zewnętrzny: spójność wyboru route i mapy, dostęp
  node’ów, dostępność tras, utrzymanie postępu i brak resetów przy ponownym wejściu.
- Priorytet testów:
  - `RouteChoiceBinding`:
    - każdy route ma przypisaną listę ścieżek,
    - dwa różne route mogą mieć ten sam path layout tylko z jawnie zapisanym
      wyjątkiem,
    - nieznany route zwraca domyślny profil.
  - `StartRunService + RunMapService` integration:
    - 020 przekazuje wybrany route do 021,
    - 021 dla różnych wybiorów daje rozróżnialne mapy,
    - fallback przy braku/nieznanym id działa bez crasha.
  - persistence:
    - zapis/odczyt state’u utrzymuje selectedRouteChoiceId,
    - odtworzenie stanu mapy zachowuje current node i progress.
  - UX:
    - `route preview` jest prezentacyjny, a `route map` interaktywny.
- Priorytet implementacji testów:
  - edutowe testy domenowe (EditMode) przed zmianami UI.
  - re-run istniejących testów 020/021 po modyfikacjach katalogu pathów.
- Jako analogię biorę pod uwagę:
  - `StartRunServiceTests`,
  - `RunMapServiceTests`,
  - `OfflineStartRunRunMapDbTests`.

## Out of Scope

- Pełna ekonomia runu (nowe waluty, sklepowe formuły, zmiany skali nagród).
- Zmiana zachowania 022 Run Battle, 023 Reward Map, 024 Run Shop poza ich obecnymi
  interfejsami wejściowymi.
- Wprowadzenie nowego AI do generacji encounterów.
- Zmiany balansu jednostek, umiejętności, cooldownów i statystyk.
- Rewizja baz danych produkcyjnych poza lokalnym, offline mode boundary.
- Zmiana prefabów/scen bez osobnego zadania i potwierdzenia.

## Further Notes

- To zadanie ma domknąć lukę gameplayową „wybieram route, ale gra nie różni zachowania”,
  bez zmieniania rdzeniowej struktury walki.
- Największe ryzyko to rozjazd nazewnictwa i oczekiwań:
  `Route` (020) to decyzja profilu początkowego,
  `Path` (021) to konkretna trajektoria mapy.
- PRD celowo zostawia przestrzeń na przyszłe rozszerzenia:
  dynamiczne generowanie node’ów, route modyfikacje przez run tags, event nodes,
  zaawansowane seedowanie.
- Wersja do grillowania: potwierdzić, ile wariantów mapy na V1 jest gameplayowo
  sensowne i czy route powinien też modyfikować np. liczność/bieg node’ów.
