using System.Text;
using Docvela.Models;

public class SvgGenerator
{
    private const int MaxCharsPerLine = 20;
    private const int LineHeight = 14; // px line height for wrapped text

    public string GenerateSvg(InputData data)
    {
        var sb = new StringBuilder();

        // SVG & Styles
        sb.AppendLine("<svg xmlns='http://www.w3.org/2000/svg' width='100%' height='100%' viewBox='0 0 2000 2000' style='border:1px solid #ccc;'>");
        sb.AppendLine("<style>");
        sb.AppendLine("  .controller { fill: #eef; stroke: #00f; stroke-width: 1; cursor: pointer; }");
        sb.AppendLine("  .service { fill: #efe; stroke: #080; stroke-width: 1; cursor: pointer; }");
        sb.AppendLine("  .endpoint { fill: #fee; stroke: #a00; stroke-width: 1; cursor: pointer; }");
        sb.AppendLine("  .label { font-size: 12px; fill: #000; user-select: none; }");
        sb.AppendLine("  .arrow { stroke: #000; stroke-width: 1.5; marker-end: url(#arrowhead); }");
        sb.AppendLine("</style>");

        // Arrowhead Marker
        sb.AppendLine("<defs>");
        sb.AppendLine("  <marker id='arrowhead' markerWidth='10' markerHeight='7' refX='10' refY='3.5' orient='auto'>");
        sb.AppendLine("    <polygon points='0 0, 10 3.5, 0 7' fill='black' />");
        sb.AppendLine("  </marker>");
        sb.AppendLine("</defs>");

        // Constants for layout
        int margin = 50;
        int boxWidth = 180;
        int controllerMarginBottom = 30;
        int verticalSpacing = 10;
        int xController = margin;
        int xService = xController + 250;

        var endpointPositions = new Dictionary<string, (int x, int y)>();
        var servicePositions = new Dictionary<string, (int x, int y)>();

        int yController = margin; // Y coordinate for controllers start

        // Draw controllers and endpoints with dynamic vertical layout
        foreach (var controller in data.Controllers)
        {
            // Wrap controller name
            var controllerNameLines = WrapLines(controller.ControllerName, MaxCharsPerLine);
            int controllerNameHeight = controllerNameLines.Count * LineHeight + 10;

            // Calculate height needed for all endpoints (each can be multiline)
            int endpointsHeight = 0;
            var endpointLineCounts = new List<int>();
            foreach (var endpoint in controller.Endpoints)
            {
                var lines = WrapLines(endpoint.MethodSignature, MaxCharsPerLine);
                endpointLineCounts.Add(lines.Count);
                endpointsHeight += Math.Max(40, lines.Count * LineHeight + 4) + verticalSpacing;
            }

            int controllerBoxHeight = controllerNameHeight + endpointsHeight + verticalSpacing;

            // Controller box
            sb.AppendLine($"<rect class='controller' x='{xController}' y='{yController}' width='{boxWidth}' height='{controllerBoxHeight}' rx='6' />");

            // Controller name text with wrapping
            int textX = xController + 10;
            int textY = yController + LineHeight; // start a bit below top edge
            sb.AppendLine($"<text class='label' x='{textX}' y='{textY}'>");
            for (int i = 0; i < controllerNameLines.Count; i++)
            {
                int dy = i == 0 ? 0 : LineHeight;
                sb.AppendLine($"<tspan x='{textX}' dy='{dy}'>{EscapeXml(controllerNameLines[i])}</tspan>");
            }
            sb.AppendLine("</text>");

            // Draw each endpoint inside controller box, below controller name
            int endpointY = yController + controllerNameHeight + verticalSpacing;
            for (int i = 0; i < controller.Endpoints.Count; i++)
            {
                var endpoint = controller.Endpoints[i];
                int lineCount = endpointLineCounts[i];
                int endpointBoxHeight = Math.Max(40, lineCount * LineHeight + 4);

                sb.AppendLine($"<rect class='endpoint' x='{xController + 20}' y='{endpointY}' width='{boxWidth - 40}' height='{endpointBoxHeight}' rx='6' />");

                // Endpoint text with wrapping
                sb.AppendLine($"<text class='label' x='{xController + 30}' y='{endpointY + LineHeight}'>");
                var endpointLines = WrapLines(endpoint.MethodSignature, MaxCharsPerLine);
                for (int j = 0; j < endpointLines.Count; j++)
                {
                    int dy = j == 0 ? 0 : LineHeight;
                    sb.AppendLine($"<tspan x='{xController + 30}' dy='{dy}'>{EscapeXml(endpointLines[j])}</tspan>");
                }
                sb.AppendLine("</text>");

                // Store center right edge of endpoint box for arrow start
                int arrowStartX = xController + 20 + (boxWidth - 40);
                int arrowStartY = endpointY + endpointBoxHeight / 2;
                string endpointId = $"{controller.ControllerName}.{endpoint.MethodSignature}";
                endpointPositions[endpointId] = (arrowStartX, arrowStartY);

                endpointY += endpointBoxHeight + verticalSpacing;
            }

            yController += controllerBoxHeight + controllerMarginBottom;
        }

        // Draw services stacked vertically on right
        int yService = margin;
        int serviceBoxHeight = 40;
        foreach (var service in data.Services)
        {
            sb.AppendLine($"<rect class='service' x='{xService}' y='{yService}' width='{boxWidth}' height='{serviceBoxHeight}' rx='6' />");
            sb.AppendLine($"<text class='label' x='{xService + 10}' y='{yService + 25}'>{EscapeXml(service.ServiceName)}</text>");

            // Store center left edge for arrow end
            int arrowEndX = xService;
            int arrowEndY = yService + serviceBoxHeight / 2;
            servicePositions[service.ServiceName] = (arrowEndX, arrowEndY);

            yService += serviceBoxHeight + verticalSpacing + 10;
        }

        // Draw arrows from endpoints to services
        foreach (var controller in data.Controllers)
        {
            foreach (var endpoint in controller.Endpoints)
            {
                string endpointId = $"{controller.ControllerName}.{endpoint.MethodSignature}";
                if (!endpointPositions.TryGetValue(endpointId, out var start)) continue;

                foreach (var call in endpoint.ServiceCalls)
                {
                    var serviceName = ExtractServiceName(call);
                    if (!servicePositions.TryGetValue(serviceName, out var end)) continue;

                    sb.AppendLine($"<line class='arrow' x1='{start.x}' y1='{start.y}' x2='{end.x}' y2='{end.y}' />");
                }
            }
        }

        // Zoom & pan script inside SVG
        sb.AppendLine(@"<script type='application/ecmascript'><![CDATA[
  let svg = document.querySelector('svg');
  let isPanning = false;
  let startPoint = {x: 0, y: 0};
  let viewBox = {x: 0, y: 0, width: 2000, height: 2000};

  svg.addEventListener('mousedown', e => {
    isPanning = true;
    startPoint = {x: e.clientX, y: e.clientY};
  });

  svg.addEventListener('mouseup', e => {
    isPanning = false;
  });

  svg.addEventListener('mouseleave', e => {
    isPanning = false;
  });

  svg.addEventListener('mousemove', e => {
    if (!isPanning) return;
    let dx = (startPoint.x - e.clientX) * (viewBox.width / svg.clientWidth);
    let dy = (startPoint.y - e.clientY) * (viewBox.height / svg.clientHeight);
    viewBox.x += dx;
    viewBox.y += dy;
    svg.setAttribute('viewBox', `${viewBox.x} ${viewBox.y} ${viewBox.width} ${viewBox.height}`);
    startPoint = {x: e.clientX, y: e.clientY};
  });

  svg.addEventListener('wheel', e => {
  e.preventDefault();
  const scaleAmount = 0.1;
  let newWidth = viewBox.width;
  let newHeight = viewBox.height;

  if (e.deltaY < 0) {
    newWidth = viewBox.width / (1 + scaleAmount);
    newHeight = viewBox.height / (1 + scaleAmount);
  } else {
    newWidth = viewBox.width * (1 + scaleAmount);
    newHeight = viewBox.height * (1 + scaleAmount);
  }

  const minViewBoxSize = 500;
  const maxViewBoxSize = 3000;
  viewBox.width = Math.min(Math.max(newWidth, minViewBoxSize), maxViewBoxSize);
  viewBox.height = Math.min(Math.max(newHeight, minViewBoxSize), maxViewBoxSize);

  svg.setAttribute('viewBox', `${viewBox.x} ${viewBox.y} ${viewBox.width} ${viewBox.height}`);
});

]]></script>");

        sb.AppendLine("</svg>");

        return sb.ToString();
    }

    private static List<string> WrapLines(string text, int maxCharsPerLine)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text))
            return lines;

        int start = 0;
        while (start < text.Length)
        {
            int length = Math.Min(maxCharsPerLine, text.Length - start);
            lines.Add(text.Substring(start, length));
            start += length;
        }
        return lines;
    }

    private static string EscapeXml(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private string ExtractServiceName(string serviceCall)
    {
        if (string.IsNullOrEmpty(serviceCall)) return "Unknown";

        int parenIndex = serviceCall.IndexOf('(');
        string methodPath = parenIndex > 0 ? serviceCall.Substring(0, parenIndex) : serviceCall;

        var parts = methodPath.Split('.');

        if (parts.Length >= 2)
        {
            return parts[parts.Length - 2];
        }

        return "Unknown";
    }
}
