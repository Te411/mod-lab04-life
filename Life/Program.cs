using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using Microsoft.Extensions.Configuration;
using ScottPlot;

namespace cli_life
{
    /// <summary>
    /// Класс, представляющий настройки
    /// </summary>
    public class Setting
    {
        /// <summary>
        /// Ширина
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Высота
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Размер ячейки
        /// </summary>
        public int CellSize { get; set; }

        /// <summary>
        /// Плотность жизни
        /// </summary>
        public double LiveDensity { get; set; }

        /// <summary>
        /// Паттерн
        /// </summary>
        public DefaultPatterns DefaultPatterns { get; set; }
    }

    /// <summary>
    /// Класс, представляющий паттерн
    /// </summary>
    public class DefaultPatterns
    {
        public string Path { get; set; }
    }


    /// <summary>
    /// Класс, представляющий ячейку
    /// </summary>
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        public bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }

    /// <summary>
    /// Класс, представляющий решетку
    /// </summary>
    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;

        /// <summary>
        /// Колонки
        /// </summary>
        public int Columns { get { return Cells.GetLength(0); } }

        /// <summary>
        /// Стобцы
        /// </summary>
        public int Rows { get { return Cells.GetLength(1); } }

        /// <summary>
        /// Ширина
        /// </summary>
        public int Width { get { return Columns * CellSize; } }

        /// <summary>
        /// Высота
        /// </summary>
        public int Height { get { return Rows * CellSize; } }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="width">ширина</param>
        /// <param name="height">высота</param>
        /// <param name="cellSize">размер ячейки</param>
        /// <param name="liveDensity">плотность жизни</param>
        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            CellSize = cellSize;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        readonly Random rand = new Random();

        /// <summary>
        /// Задать ячейку
        /// </summary>
        /// <param name="liveDensity">плотность жизни</param>
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        /// <summary>
        /// Обновить состояние ячейки
        /// </summary>
        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }

        /// <summary>
        /// Вычислить соседей ячеек
        /// </summary>
        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }

        /// <summary>
        /// Сохранить состояние системы в файл
        /// </summary>
        /// <param name="filename">Название файла</param>
        public void SaveToFile(string filename)
        {
            using (var writer = new StreamWriter(filename))
            {
                writer.WriteLine($"{Columns} {Rows}");
                for (var y = 0; y < Rows; y++)
                {
                    var line = new StringBuilder();
                    for (var x = 0; x < Columns; x++)
                    {
                        line.Append(Cells[x, y].IsAlive ? '1' : '0');
                    }

                    writer.WriteLine(line);
                }
            }
        }

        /// <summary>
        /// Загрузить состояние системы из файла
        /// </summary>
        /// <param name="filename">Название файла</param>
        public static Board LoadBoardFromFile(string filename, int cellSize)
        {
            var lines = File.ReadAllLines(filename);
            var dimensions = lines[0].Split(' ');
            var columns = int.Parse(dimensions[0]);
            var rows = int.Parse(dimensions[1]);
            var board = new Board(columns, rows, cellSize);

            for (var y = 0; y < rows; y++)
            {
                var line = lines[y + 1];
                for (var x = 0; x < columns; x++)
                {
                    board.Cells[x, y].IsAlive = line[x] == '1';
                }
            }
            return board;
        }

        /// <summary>
        /// Загрузить модель 'Фигура-колония' из файла
        /// </summary>
        /// <param name="filename">Название файла</param>
        /// <param name="cellSize">размер ячейки</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static Board LoadModelFromFile(string filename, int cellSize)
        {
            var lines = File.ReadAllLines(filename);

            var rows = lines.Length;
            var columns = lines[0].Length;

            if (lines.Any(line => line.Length != columns))
            {
                throw new InvalidDataException("Некорректный формат паттерна");
            }

            var board = new Board(columns, rows, cellSize);

            for (var y = 0; y < rows; y++)
            {
                for (var x = 0; x < columns; x++)
                {
                    if (x < board.Columns && y < board.Rows)
                    {
                        board.Cells[x, y].IsAlive = lines[y][x] == '1';
                    }
                }
            }
            return board;
        }
    }
    public class Program
    {
        public static Board board;
        static List<int> liveCellsHistory = new List<int>();

        public static Dictionary<string, (string Type, string[] Pattern)> Figures = new Dictionary<string, (string, string[])>
        {
            { "block", ("Устойчивая", new[] { "11", "11" }) },
            { "blinker", ("Периодическая", new[] { "111" }) },
            { "beacon", ("Периодическая", new[] { "1100", "1000", "0010", "0011" }) },
            { "hive", ("Устойчивая", new[] { "010", "101", "010" }) },
            { "pond", ("Устойчивая", new[] { "0110", "1001", "1001", "0110" }) },
            { "loaf", ("Устойчивая", new[] { "0110", "1001", "0101", "0010" }) }
        };

        /// <summary>
        /// Перезагрузить настройки
        /// </summary>
        /// <param name="settingsPath"></param>
        static public void Reset(string settingsPath = "../../../appsettings.json")
        {
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<Setting>(json);

                board = new Board(
                     width: settings.Width,
                     height: settings.Height,
                     cellSize: settings.CellSize,
                     liveDensity: settings.LiveDensity);
            }
            else
            {
                board = new Board(
                     width: 100,
                    height: 20,
                    cellSize: 1,
                    liveDensity: 0.1 
                    );
            }
        }

        static void Render()
        {
            Console.WriteLine("Игра 'Жизнь'");
            Console.WriteLine("S - Сохранить текущее состояние");
            Console.WriteLine("L - Загрузить состояние из файла");
            Console.WriteLine("R - Сбросить с случайной генерацией");
            Console.WriteLine("P - Загрузить модель 'фигура-колония'");
            Console.WriteLine("Q - Завершить");
            Console.WriteLine();

            for (var row = 0; row < board.Rows; row++)
            {
                for (var col = 0; col < board.Columns; col++)   
                {
                    var cell = board.Cells[col, row];
                    Console.Write(cell.IsAlive ? '*' : ' ');
                }
                Console.Write('\n');
            }
        }

        /// <summary>
        /// Загрузить состояние из файла
        /// </summary>
        /// <param name="filename"></param>
        static void LoadFromFile(string filename)
        {
            try
            {
                var cellSize = board?.CellSize ?? 1;
                board = Board.LoadBoardFromFile(filename, cellSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки файла: {ex.Message}");
                Thread.Sleep(2000);
            }
        }

        /// <summary>
        /// Загрузить модель
        /// </summary>
        /// <param name="modelName"></param>
        static void LoadModel(string modelName)
        {
            var json = File.ReadAllText("../../../appsettings.json");
            var settings = JsonSerializer.Deserialize<Setting>(json);
            var patternPath = Path.Combine(settings.DefaultPatterns.Path, $"{modelName}.txt");

            if (File.Exists(patternPath))
            {
                var cellSize = board?.CellSize ?? 1;
                board = Board.LoadModelFromFile(patternPath, cellSize);
            }
            else
            {
                Console.WriteLine($"Модель {modelName} не найдена");
                Thread.Sleep(2000);
            }
        }

        /// <summary>
        /// Получить количество
        /// </summary>
        /// <returns></returns>
        public static int GetCountLiveCells()
        {
            var count = 0;
            foreach (Cell cell in board.Cells)
            {
                if (cell.IsAlive)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Получить количество групп
        /// </summary>
        /// <returns></returns>
        public static int GetCountCombinations()
        {
            var visited = new bool[board.Columns, board.Rows];
            var count = 0;

            for (var x = 0; x < board.Columns; x++)
            {
                for (var y = 0; y < board.Rows; y++)
                {
                    if (!visited[x, y] && board.Cells[x, y].IsAlive)
                    {
                        count++;
                        ExploreGroup(x, y, visited);
                    }
                }
            }
            return count;
        }

        public static void ExploreGroup(int x, int y, bool[,] visited)
        {
            if (x < 0 || y < 0 || x >= board.Columns || y >= board.Rows || visited[x, y] || !board.Cells[x, y].IsAlive)
            {
                return;
            }

            visited[x, y] = true;

            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    ExploreGroup(x + dx, y + dy, visited);
                }
            }
        }

        /// <summary>
        /// Анализ решетки
        /// </summary>
        public static void AnalyzeBoard()
        {
            var liveCells = GetCountLiveCells();
            var combinations = GetCountCombinations();
            var figures = FindFigures(board);
            Console.WriteLine($"Количество живых клеток: {liveCells}");
            Console.WriteLine($"Количество комбинаций: {combinations}");
            if (figures.Any())
            {
                Console.WriteLine("Найдены фигуры:");
                foreach (var fig in figures.Distinct())
                {
                    Console.WriteLine($"- {fig}");
                }
            }
            Thread.Sleep(2000);
        }

        /// <summary>
        /// Поиск соответсвий по шаблонам
        /// </summary>
        /// <param name="board"></param>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static bool FindPattern(Board board, int x0, int y0, string[] pattern)
        {
            var patternHeight = pattern.Length;
            var patternWidth = pattern[0].Length;

            for (var y = 0; y < patternHeight; y++)
            {
                for (var x = 0; x < patternWidth; x++)
                {
                    var boardX = x0 + x;
                    var boardY = y0 + y;
                    var cellAlive = boardX < board.Columns && boardY < board.Rows
                                     && board.Cells[boardX, boardY].IsAlive;
                    var expectedAlive = pattern[y][x] == '1';
                    if (cellAlive != expectedAlive)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Поиск фигур-колоний на решетке
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        public static List<string> FindFigures(Board board)
        {
            var found = new List<string>();
            foreach (var figure in Figures)
            {
                var name = figure.Key;
                var pattern = figure.Value.Pattern;
                var patternWidth = pattern[0].Length;
                var patternHeight = pattern.Length;

                for (var y = 0; y <= board.Rows - patternHeight; y++)
                {
                    for (var x = 0; x <= board.Columns - patternWidth; x++)
                    {
                        if (FindPattern(board, x, y, pattern))
                        {
                            found.Add($"{name} ({figure.Value.Type})");
                        }
                    }
                }
            }
            return found;
        }

        /// <summary>
        /// Проверка на стабильность
        /// </summary>
        /// <param name="history"></param>
        /// <param name="stabilityThreshold"></param>
        /// <returns></returns>
        public static bool IsStable(List<int> history, int stabilityThreshold = 5)
        {
            if (history.Count < stabilityThreshold) 
            {
                return false;
            }

            var lastValue = history[history.Count - 1];

            for (int i = 1; i < stabilityThreshold; i++)
            {
                if (history[history.Count - 1 - i] != lastValue)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Запустить эксперимент с заданой плотностью
        /// </summary>
        /// <param name="density"></param>
        /// <returns></returns>
        public static int RunExperiment(double density)
        {
            var experimentBoard = new Board(50, 20, 1, density);
            var history = new List<int>();

            for (var i = 0; i < 1000; i++)
            {
                history.Add(GetCountLiveCells(experimentBoard));
                experimentBoard.Advance();

                if (IsStable(history))
                {
                    return i - 4;
                }
            }
            return 1000;
        }

        /// <summary>
        /// Получить количество живых ячеек
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int GetCountLiveCells(Board b)
        {
            var count = 0;
            foreach (var cell in b.Cells)
            {
                if (cell.IsAlive)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Построить график
        /// </summary>
        public static void DrawPlot()
        {
            var densities = new List<double>();
            var generations = new List<int>();

            for (double density = 0.1; density <= 0.9; density += 0.05)
            {
                var totalGenerations = 0;
                var experimentsCount = 5;

                for (int i = 0; i < experimentsCount; i++)
                {
                    totalGenerations += RunExperiment(density);
                }

                var avgGenerations = totalGenerations / experimentsCount;
                densities.Add(density);
                generations.Add(avgGenerations);
            }

            File.WriteAllText("../../../data.txt",
                string.Join(Environment.NewLine,
                densities.Zip(generations, (d, g) => $"{d}\t{g}")));

            var plot = new Plot();

            var xValues = densities.Select(d => (double)d).ToArray();
            var yValues = generations.Select(g => (double)g).ToArray();
            var sig = plot.Add.Scatter(xValues, yValues);

            plot.XLabel("Начальная плотность");
            plot.YLabel("Поколения до стабилизации");
            plot.Title("Зависимость времени стабилизации от плотности");
            plot.SavePng("../../../plot.png", 800, 600);
        }

        static void Main(string[] args)
        {
            DrawPlot();
            Reset();
            var running = true;

            while (running)
            {
                Console.Clear();
                Render();
                liveCellsHistory.Add(GetCountLiveCells());

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.S:
                            Console.Write("\nВведите имя файла для сохранения: ");
                            var saveFile = Console.ReadLine();
                            board.SaveToFile($"../../../Models/{saveFile}");
                            break;

                        case ConsoleKey.L:
                            Console.Write("\nВведите имя файла для загрузки: ");
                            var loadFile = Console.ReadLine();
                            LoadFromFile($"../../../Models/{loadFile}");
                            break;

                        case ConsoleKey.R:
                            Reset();
                            break;

                        case ConsoleKey.P:
                            Console.Write("\nВведите имя модели (box/block/hive/pond/loaf/blinker/beacon): ");
                            var patternName = Console.ReadLine();
                            LoadModel(patternName);
                            break;

                        case ConsoleKey.Q:
                            running = false;
                            break;
                    }
                }

                board.Advance();
                AnalyzeBoard();
                Thread.Sleep(1000);
            }
        }
    }
}