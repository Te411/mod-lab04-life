using cli_life;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace cli_life.Tests
{
    [TestClass]
    public class CellTests
    {
        /// <summary>
        /// Проверка начального состояния клетки
        /// </summary>
        [TestMethod]
        public void CellDefaultStateDead()
        {
            var cell = new Cell();
            Assert.IsFalse(cell.IsAlive);
        }

        /// <summary>
        /// Проверка оживления мертвой клетки
        /// </summary>
        [TestMethod]
        public void DeadCellNeighborsBecomesAlive()
        {
            var cell = new Cell { IsAlive = false };
            cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 3));

            cell.DetermineNextLiveState();

            Assert.IsTrue(cell.IsAliveNext);
        }

        /// <summary>
        /// Проверка выживания живой клетки
        /// </summary>
        [TestMethod]
        public void LiveCellNeighborsStaysAlive()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 2));

            cell.DetermineNextLiveState();

            Assert.IsTrue(cell.IsAliveNext);
        }

        /// <summary>
        /// Проверка гибели живой клетки
        /// </summary>
        [TestMethod]
        public void LiveCellNeighborsDies()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 4));

            cell.DetermineNextLiveState();

            Assert.IsFalse(cell.IsAliveNext);
        }
    }

    [TestClass]
    public class BoardTests
    {
        /// <summary>
        /// Проверка количества соседей у угловой клетки
        /// </summary>
        [TestMethod]
        public void CornerCellHasNeighbors()
        {
            var board = new Board(3, 3, 1);
            var cornerCell = board.Cells[0, 0];

            Assert.AreEqual(8, cornerCell.neighbors.Count);
        }

        /// <summary>
        /// Проверка невозможности найти паттерн больше Board
        /// </summary>
        [TestMethod]
        public void LargePatternOnSmallBoardNotFound()
        {
            var board = new Board(2, 2, 1);
            string[] largePattern = { "111", "111", "111" };

            bool found = Program.FindPattern(board, 0, 0, largePattern);
            Assert.IsFalse(found);
        }

        /// <summary>
        /// Проверка корректного размера Board
        /// </summary>
        [TestMethod]
        public void BoardCorrectDimensions()
        {
            var board = new Board(100, 20, 1);

            Assert.AreEqual(100, board.Columns);
            Assert.AreEqual(20, board.Rows);
        }

        /// <summary>
        /// Проверка корректности сохрания и загрузки файла
        /// </summary>
        [TestMethod]
        public void SaveAndLoadBoardFile()
        {
            var originalBoard = new Board(10, 10, 1);
            originalBoard.Randomize(0.5);
            string testFile = "test_board.txt";

            originalBoard.SaveToFile(testFile);
            var loadedBoard = Board.LoadBoardFromFile(testFile, 1);

            for (int x = 0; x < originalBoard.Columns; x++)
            {
                for (int y = 0; y < originalBoard.Rows; y++)
                {
                    Assert.AreEqual(
                        originalBoard.Cells[x, y].IsAlive,
                        loadedBoard.Cells[x, y].IsAlive);
                }
            }

            File.Delete(testFile);
        }

        /// <summary>
        /// Проверка корректности загрузки модели 'Фигура-колония'
        /// </summary>
        [TestMethod]
        public void LoadModelFromFileCorrect()
        {
            string testFile = "test_model.txt";
            File.WriteAllText(testFile, "010\n101\n010\n");

            var board = Board.LoadModelFromFile(testFile, 1);

            Assert.IsFalse(board.Cells[0, 0].IsAlive);
            Assert.IsTrue(board.Cells[1, 0].IsAlive);
            Assert.IsFalse(board.Cells[2, 0].IsAlive);

            Assert.IsTrue(board.Cells[0, 1].IsAlive);
            Assert.IsFalse(board.Cells[1, 1].IsAlive);
            Assert.IsTrue(board.Cells[2, 1].IsAlive);

            File.Delete(testFile);
        }
    }

    [TestClass]
    public class ProgramTests
    {
        /// <summary>
        /// Проверка подсчета живых ячеек на пустом Board
        /// </summary>
        [TestMethod]
        public void EmptyBoardNoLiveCells()
        {
            var board = new Board(10, 10, 1);
            foreach (var cell in board.Cells) cell.IsAlive = false;

            var count = 0;
            foreach (var cell in board.Cells)
            {
                if (cell.IsAlive) count++;
            }

            Assert.AreEqual(0, count);
        }

        /// <summary>
        /// Проверка подсчета живых ячеек на полном Board
        /// </summary>
        [TestMethod]
        public void FullBoardAllCellsAlive()
        {
            var board = new Board(10, 10, 1);
            foreach (var cell in board.Cells) cell.IsAlive = true;

            var count = Program.GetCountLiveCells(board);

            Assert.AreEqual(100, count);
        }

        /// <summary>
        /// Проверка поиска модели блок 'Фигура-колония'
        /// </summary>
        [TestMethod]
        public void FindPatternBlockFound()
        {
            var board = new Board(4, 4, 1);

            board.Cells[1, 1].IsAlive = true;
            board.Cells[1, 2].IsAlive = true;
            board.Cells[2, 1].IsAlive = true;
            board.Cells[2, 2].IsAlive = true;

            bool found = Program.FindPattern(board, 1, 1, Program.Figures["block"].Pattern);

            Assert.IsTrue(found);
        }

        /// <summary>
        /// Проверка стабильности при одинаковых значениях
        /// </summary>
        [TestMethod]
        public void StableHistoryReturnsTrue()
        {
            var history = new List<int> { 10, 10, 10, 10, 10 };

            bool isStable = Program.IsStable(history);

            Assert.IsTrue(isStable);
        }

        /// <summary>
        /// Проверка нестабильности при разных значениях
        /// </summary>
        [TestMethod]
        public void UnstableHistoryReturnsFalse()
        {
            var history = new List<int> { 10, 11, 10, 11, 10 };

            bool isStable = Program.IsStable(history);

            Assert.IsFalse(isStable);
        }

        /// <summary>
        /// Проверка корректной загрузки настройки
        /// </summary>
        [TestMethod]
        public void LoadSettingsCreatesCorrectBoard()
        {
            var settings = new Setting
            {
                Width = 80,
                Height = 40,
                CellSize = 2,
                LiveDensity = 0.2,
                DefaultPatterns = new DefaultPatterns { Path = "./Models" }
            };
            string json = JsonSerializer.Serialize(settings);
            File.WriteAllText("test_settings.json", json);

            Program.Reset("test_settings.json");

            Assert.IsNotNull(Program.board);
            Assert.AreEqual(80, Program.board.Width);
            Assert.AreEqual(40, Program.board.Height);
            Assert.AreEqual(2, Program.board.CellSize);

            File.Delete("test_settings.json");
        }
    }
}