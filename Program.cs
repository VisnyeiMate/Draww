using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

class Program
{
    static char drawCharacter = '█';
    static ConsoleColor drawColor = ConsoleColor.White;
    static int x = Console.WindowWidth / 2;
    static int y = Console.WindowHeight / 2;
    static bool exit = false;
    static bool inDrawingMode = false;

    static string[] menuOptions = { "Kezdés", "Szerkesztés", "Törlés", "Kilépés" };
    static int selectedIndex = 0;

    static List<Point> rajz = new List<Point>();

    static string saveDirectory = "rajzok";

    static void Main()
    {
        Console.CursorVisible = false;
        using (var context = new DrawingContext())
        {
            context.Database.Migrate();
        }

        ShowMainMenu();
        while (!exit)
        {
            if (inDrawingMode)
            {
                DrawingMode();
            }
            else
            {
                ShowMainMenu();
            }
        }
    }

    static void ShowMainMenu()
    {
        DrawMenuBorder();
        ShowMenu(menuOptions, selectedIndex);
        while (!inDrawingMode && !exit)
        {
            var key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.UpArrow && selectedIndex > 0) selectedIndex--;
            else if (key.Key == ConsoleKey.DownArrow && selectedIndex < menuOptions.Length - 1) selectedIndex++;
            else if (key.Key == ConsoleKey.Enter)
            {
                switch (selectedIndex)
                {
                    case 0:
                        NewDrawing();
                        break;
                    case 1:
                        EditDrawing();
                        break;
                    case 2:
                        DeleteDrawing();
                        break;
                    case 3:
                        exit = true;
                        break;
                }
            }
            DrawMenuBorder();
            ShowMenu(menuOptions, selectedIndex);
        }
    }

    static void NewDrawing()
    {
        rajz.Clear();
        x = Console.WindowWidth / 2;
        y = Console.WindowHeight / 2;
        inDrawingMode = true;
        Console.Clear();
    }

    static void EditDrawing()
    {
        using (var context = new DrawingContext())
        {
            var drawings = context.Drawings.ToArray();
            if (drawings.Length == 0)
            {
                Console.Clear();
                Console.WriteLine("Nincs elérhető rajz a szerkesztéshez.");
                Console.ReadKey();
                return;
            }

            var selectedDrawing = SelectDrawing(drawings);
            LoadDrawing(selectedDrawing);
            inDrawingMode = true;
            Console.Clear();
        }
    }

    static void DeleteDrawing()
    {
        using (var context = new DrawingContext())
        {
            var drawings = context.Drawings.ToArray();
            if (drawings.Length == 0)
            {
                Console.Clear();
                Console.WriteLine("Nincs elérhető rajz a törléshez.");
                Console.ReadKey();
                return;
            }

            var selectedDrawing = SelectDrawing(drawings);
            if (selectedDrawing == null)
            {
                Console.Clear();
                Console.WriteLine("A kiválasztott rajz nem található.");
                Console.ReadKey();
                return;
            }

            context.Drawings.Remove(selectedDrawing);
            context.SaveChanges();
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("A fájl törölve lett.");
            Console.ResetColor();
            Console.ReadKey();
        }
    }

    static Drawing? SelectDrawing(Drawing[] drawings)
    {
        int drawingIndex = 0;
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Válassz egy rajzot:");
            for (int i = 0; i < drawings.Length; i++)
            {
                if (i == drawingIndex)
                    Console.ForegroundColor = ConsoleColor.Blue;
                else
                    Console.ResetColor();
                Console.WriteLine($"Rajz {i + 1}: {drawings[i].Name}");
            }

            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.UpArrow && drawingIndex > 0) drawingIndex--;
            else if (key.Key == ConsoleKey.DownArrow && drawingIndex < drawings.Length - 1) drawingIndex++;
            else if (key.Key == ConsoleKey.Enter)
                return drawings[drawingIndex];
            else if (key.Key == ConsoleKey.Escape)
                return null;
        }
    }

    static void LoadDrawing(Drawing? drawing)
    {
        if (drawing == null)
        {
            Console.Clear();
            Console.WriteLine("A kiválasztott rajz nem található.");
            Console.ReadKey();
            inDrawingMode = false;
            ShowMainMenu();
            return;
        }

        rajz.AddRange(drawing.Points);
        RedrawSavedDrawing();

       
    }

    static void SaveDrawing(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            Console.Clear();
            Console.WriteLine("Érvénytelen fájlnév.");
            Console.ReadKey();
            return;
        }

        using (var context = new DrawingContext())
        {
            var newDrawing = new Drawing
            {
                Name = fileName,
                Points = new List<Point>()
            };

            foreach (var point in rajz)
            {
                if (!newDrawing.Points.Any(p => p.Id == point.Id))
                {
                    newDrawing.Points.Add(point);
                }
            }

            context.Drawings.Add(newDrawing);
            context.SaveChanges();
        }
    }

    static void DrawingMode()
    {
        bool drawing = true;
        Console.Clear();

        while (drawing)
        {
            RedrawSavedDrawing();
            var key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.Spacebar)
            {
                Console.SetCursorPosition(x, y);
                Console.ForegroundColor = drawColor;
                Console.Write(drawCharacter);
                Console.ResetColor();
                rajz.Add(new Point { X = x, Y = y, Character = drawCharacter, Color = drawColor });
            }
            else if (key.Key == ConsoleKey.Escape)
            {
                Console.Clear();
                Console.Write("Add meg a mentés nevét: ");
                string? fileName = Console.ReadLine();
                SaveDrawing(fileName);
                drawing = false;
                inDrawingMode = false;
                Console.Clear();
            }
            else if (key.Key == ConsoleKey.F1) drawCharacter = '█';
            else if (key.Key == ConsoleKey.F2) drawCharacter = '▓';
            else if (key.Key == ConsoleKey.F3) drawCharacter = '▒';
            else if (key.Key == ConsoleKey.F4) drawCharacter = '░';
            else if (key.Key >= ConsoleKey.D0 && key.Key <= ConsoleKey.D9) drawColor = (ConsoleColor)(key.Key - ConsoleKey.D0);
            else MoveCursor(ref x, ref y, key);
        }
    }

    static void RedrawSavedDrawing()
    {
        foreach (var point in rajz)
        {
            Console.SetCursorPosition(point.X, point.Y);
            Console.ForegroundColor = point.Color;
            Console.Write(point.Character);
        }


        Console.SetCursorPosition(x, y);
        Console.ForegroundColor = drawColor;
        Console.Write(drawCharacter);

        Console.ResetColor();
    }

    static void MoveCursor(ref int x, ref int y, ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow: if (x > 1) x--; break;
            case ConsoleKey.RightArrow: if (x < Console.WindowWidth - 2) x++; break;
            case ConsoleKey.UpArrow: if (y > 1) y--; break;
            case ConsoleKey.DownArrow: if (y < Console.WindowHeight - 2) y++; break;
        }
    }

    static void ShowMenu(string[] options, int selectedIndex)
    {
        int startY = Console.WindowHeight / 2 - options.Length / 2;
        for (int i = 0; i < options.Length; i++)
        {
            Console.SetCursorPosition(Console.WindowWidth / 2 - options[i].Length / 2, startY + i);
            if (i == selectedIndex)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.WriteLine(options[i]);
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine(options[i]);
            }
        }
    }

    static void DrawMenuBorder()
    {
        int menuWidth = 24;
        int menuHeight = menuOptions.Length + 2;
        int startX = Console.WindowWidth / 2 - menuWidth / 2;
        int startY = Console.WindowHeight / 2 - menuHeight / 2;

        Console.SetCursorPosition(startX, startY);
        Console.Write("╔");
        for (int i = 0; i < menuWidth - 2; i++) Console.Write("═");
        Console.Write("╗");

        for (int i = 1; i < menuHeight - 1; i++)
        {
            Console.SetCursorPosition(startX, startY + i);
            Console.Write("║");
            Console.SetCursorPosition(startX + menuWidth - 1, startY + i);
            Console.Write("║");
        }

        Console.SetCursorPosition(startX, startY + menuHeight - 1);
        Console.Write("╚");
        for (int i = 0; i < menuWidth - 2; i++) Console.Write("═");
        Console.Write("╝");
    }
}

public class Drawing
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Point> Points { get; set; } = new List<Point>();
}

public class Point
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public char Character { get; set; }
    public ConsoleColor Color { get; set; }
}

public class DrawingContext : DbContext
{
    public DbSet<Drawing> Drawings { get; set; }
    public DbSet<Point> Points { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=drawings.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Point>()
            .HasKey(p => p.Id);
        modelBuilder.Entity<Drawing>()
            .HasMany(d => d.Points)
            .WithOne()
            .HasForeignKey(p => p.Id);
    }
}
