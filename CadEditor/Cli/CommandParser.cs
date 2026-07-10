using System.Globalization;
using System.Collections.ObjectModel;
using System.Linq;
using CadEditor.Commands;
using CadEditor.Models;

namespace CadEditor.Cli;

public static class CommandParser
{
    public static CliParseResult Parse(string input, ObservableCollection<Shape> shapes)
    {
        ArgumentNullException.ThrowIfNull(shapes);

        if (string.IsNullOrWhiteSpace(input))
            return CliParseResult.Fail("Lệnh trống.");

        var tokens = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var keyword = tokens[0].ToUpperInvariant();

        return keyword switch
        {
            "LINE" => ParseLine(tokens, shapes),
            "CIRCLE" => ParseCircle(tokens, shapes),
            "RECT" => ParseRect(tokens, shapes),
            "ARC" => ParseArc(tokens, shapes),
            "DELETE" => ParseDelete(tokens, shapes),
            "UNDO" => tokens.Length == 1
                ? CliParseResult.Ok(new UndoCliCommand())
                : CliParseResult.Fail("UNDO không nhận tham số."),
            "REDO" => tokens.Length == 1
                ? CliParseResult.Ok(new RedoCliCommand())
                : CliParseResult.Fail("REDO không nhận tham số."),
            _ => CliParseResult.Fail(
                $"Lệnh không hợp lệ: '{tokens[0]}'. Các lệnh hỗ trợ: LINE, CIRCLE, RECT, ARC, DELETE, UNDO, REDO.")
        };
    }

    private static CliParseResult ParseLine(string[] tokens, ObservableCollection<Shape> shapes)
    {
        if (tokens.Length != 3)
            return CliParseResult.Fail("Cú pháp: LINE x1,y1 x2,y2");
        if (!TryParsePoint(tokens[1], out var start))
            return CliParseResult.Fail($"Toạ độ điểm đầu không hợp lệ: '{tokens[1]}'. Định dạng: x,y");
        if (!TryParsePoint(tokens[2], out var end))
            return CliParseResult.Fail($"Toạ độ điểm cuối không hợp lệ: '{tokens[2]}'. Định dạng: x,y");

        return CreateAddShapeCommand(shapes, new LineShape(start, end));
    }

    private static CliParseResult ParseCircle(string[] tokens, ObservableCollection<Shape> shapes)
    {
        if (tokens.Length != 3)
            return CliParseResult.Fail("Cú pháp: CIRCLE cx,cy Rr");
        if (!TryParsePoint(tokens[1], out var center))
            return CliParseResult.Fail($"Toạ độ tâm không hợp lệ: '{tokens[1]}'. Định dạng: x,y");
        if (!TryParseRadius(tokens[2], out var radius))
            return CliParseResult.Fail($"Bán kính không hợp lệ: '{tokens[2]}'. Định dạng: R<số>, ví dụ R3");

        return CreateAddShapeCommand(shapes, new CircleShape(center, radius));
    }

    private static CliParseResult ParseRect(string[] tokens, ObservableCollection<Shape> shapes)
    {
        if (tokens.Length != 3)
            return CliParseResult.Fail("Cú pháp: RECT x,y w,h");
        if (!TryParsePoint(tokens[1], out var topLeft))
            return CliParseResult.Fail($"Toạ độ góc không hợp lệ: '{tokens[1]}'. Định dạng: x,y");
        if (!TryParseSize(tokens[2], out var width, out var height))
            return CliParseResult.Fail($"Kích thước không hợp lệ: '{tokens[2]}'. Định dạng: w,h");

        return CreateAddShapeCommand(shapes, new RectangleShape(topLeft, width, height));
    }

    private static CliParseResult ParseArc(string[] tokens, ObservableCollection<Shape> shapes)
    {
        if (tokens.Length != 5)
            return CliParseResult.Fail("Cú pháp: ARC cx,cy Rr startDeg endDeg");
        if (!TryParsePoint(tokens[1], out var center))
            return CliParseResult.Fail($"Toạ độ tâm không hợp lệ: '{tokens[1]}'. Định dạng: x,y");
        if (!TryParseRadius(tokens[2], out var radius))
            return CliParseResult.Fail($"Bán kính không hợp lệ: '{tokens[2]}'. Định dạng: R<số>, ví dụ R50");
        if (!TryParseDouble(tokens[3], out var startDeg))
            return CliParseResult.Fail($"Góc bắt đầu không hợp lệ: '{tokens[3]}'");
        if (!TryParseDouble(tokens[4], out var endDeg))
            return CliParseResult.Fail($"Góc kết thúc không hợp lệ: '{tokens[4]}'");

        return CreateAddShapeCommand(shapes, new ArcShape(center, radius, startDeg, endDeg));
    }

    private static CliParseResult ParseDelete(string[] tokens, ObservableCollection<Shape> shapes)
    {
        if (tokens.Length != 2)
            return CliParseResult.Fail("Cú pháp: DELETE <id>");
        if (!Guid.TryParse(tokens[1], out var id))
            return CliParseResult.Fail($"Id không hợp lệ: '{tokens[1]}'. Id phải là GUID (xem trong log sau khi tạo shape).");

        var target = shapes.FirstOrDefault(shape => shape.Id == id);
        if (target == null)
            return CliParseResult.Fail($"Không tìm thấy shape với id={id}.");

        return CliParseResult.Ok(new ExecuteEditorCliCommand(new DeleteShapeCommand(shapes, target)));
    }

    private static CliParseResult CreateAddShapeCommand(ObservableCollection<Shape> shapes, Shape shape) =>
        CliParseResult.Ok(new ExecuteEditorCliCommand(new AddShapeCommand(shapes, shape)));

    private static bool TryParsePoint(string token, out Point2D point)
    {
        point = default;
        var parts = token.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return false;
        if (!TryParseDouble(parts[0], out var x)) return false;
        if (!TryParseDouble(parts[1], out var y)) return false;
        point = new Point2D(x, y);
        return true;
    }

    private static bool TryParseSize(string token, out double width, out double height)
    {
        width = height = 0;
        var parts = token.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return false;
        if (!TryParseDouble(parts[0], out width)) return false;
        if (!TryParseDouble(parts[1], out height)) return false;
        return true;
    }

    private static bool TryParseRadius(string token, out double radius)
    {
        radius = 0;
        if (token.Length < 2) return false;
        if (token[0] != 'R' && token[0] != 'r') return false;
        return TryParseDouble(token[1..], out radius);
    }

    private static bool TryParseDouble(string token, out double value) =>
        double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}
