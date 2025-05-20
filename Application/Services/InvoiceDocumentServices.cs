using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using QuestPDF.Elements;
using Core.Models;

namespace Application.Services
{
    public class InvoiceDocumentService: IDocument
    {
        private readonly InvoiceData _data;
        private readonly DatosEmpresa _datosEmpresa;

        public InvoiceDocumentService(InvoiceData data, DatosEmpresa datosEmpresa)
        {
            _data = data;
            _datosEmpresa = datosEmpresa;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        //public void Compose(IDocumentContainer container)
        //{
        //    container.Page(page =>
        //    {
        //        page.Size(PageSizes.A4);
        //        page.Margin(30);
        //        page.DefaultTextStyle(x => x.FontSize(12));

        //        page.Header()
        //            .Row(row =>
        //            {
        //                row.RelativeColumn()
        //                    .Column(col =>
        //                    {
        //                        col.Item().Text("Mi Empresa S.A.").FontSize(20).Bold();
        //                        col.Item().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}");
        //                    });

        //                row.ConstantColumn(200)
        //                    .Column(col =>
        //                    {
        //                        col.Item().Text($"Cliente: {_data.ClientName}");
        //                        col.Item().Text($"Correo: {_data.ClientEmail}");
        //                    });
        //            });

        //        page.Content()
        //            .PaddingVertical(10)
        //            .Column(col =>
        //            {
        //                col.Item().Element(x => x.Text("Detalle de la compra").FontSize(16).Bold());

        //                col.Item().Table(table =>
        //                {
        //                    // Definir columnas
        //                    table.ColumnsDefinition(columns =>
        //                    {
        //                        columns.RelativeColumn(3); // Producto
        //                        columns.RelativeColumn(1); // Cantidad
        //                        columns.RelativeColumn(2); // Precio Unitario
        //                        columns.RelativeColumn(2); // Subtotal
        //                    });

        //                    // Header de la tabla
        //                    table.Header(header =>
        //                    {
        //                        header.Cell().Element(CellStyle).Text("Producto").SemiBold();
        //                        header.Cell().Element(CellStyle).AlignCenter().Text("Cantidad").SemiBold();
        //                        header.Cell().Element(CellStyle).AlignRight().Text("Precio Unitario").SemiBold();
        //                        header.Cell().Element(CellStyle).AlignRight().Text("Subtotal").SemiBold();

        //                        static IContainer CellStyle(IContainer container)
        //                        {
        //                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
        //                        }
        //                    });

        //                    // Detalle de productos
        //                    foreach (var item in _data.Items)
        //                    {
        //                        table.Cell().Text(item.ProductName);
        //                        table.Cell().AlignCenter().Text(item.Quantity.ToString());
        //                        table.Cell().AlignRight().Text($"{item.UnitPrice:C}");
        //                        table.Cell().AlignRight().Text($"{item.Subtotal:C}");
        //                    }
        //                });
        //            });

        //        page.Footer()
        //            .AlignRight()
        //            .Text(text =>
        //            {
        //                text.Span("Total: ").Bold();
        //                text.Span($"{_data.TotalAmount:C}").FontColor(Colors.Green.Darken2).Bold();
        //            });
        //    });
        //}

        //public void Compose(IDocumentContainer container)
        //{
        //    container.Page(page =>
        //    {
        //        page.Size(PageSizes.A4);
        //        page.Margin(30);
        //        page.DefaultTextStyle(x => x.FontSize(12));

        //        // HEADER
        //        page.Header().Row(row =>
        //        {
        //            row.RelativeColumn()
        //                .Column(col =>
        //                {
        //                    col.Item().Text("Factura de Venta").FontSize(20).Bold();
        //                    col.Item().Text($"N° Factura: 0001");
        //                    col.Item().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}");
        //                });

        //            row.ConstantColumn(200)
        //                .AlignRight()
        //                .Column(col =>
        //                {
        //                    col.Item().Text("Mi Empresa S.A.").FontSize(16).Bold();
        //                });
        //        });

        //        // CLIENTE
        //        page.Content().PaddingVertical(10).Column(col =>
        //        {

        //            col.Item().PaddingTop(15);

        //            col.Item().Table(table =>
        //            {
        //                table.ColumnsDefinition(columns =>
        //                {
        //                    columns.RelativeColumn(2); // Producto
        //                    columns.RelativeColumn(2); // Cantidad
        //                });

        //                // HEADER AZUL
        //                table.Header(header =>
        //                {
        //                    header.Cell().Element(CellStyle).Background(Colors.Blue.Medium).AlignCenter().Text("Cliente").FontColor(Colors.White).SemiBold();
        //                    header.Cell().Element(CellStyle).Background(Colors.Blue.Medium).AlignCenter().Text("Documento").FontColor(Colors.White).SemiBold();

        //                    static IContainer CellStyle(IContainer container) =>
        //                        container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).PaddingHorizontal(2);
        //                });

        //                    table.Cell().AlignCenter().Text($"{ _data.ClientName}");
        //                    table.Cell().AlignCenter().Text($"12345678");

        //            });

        //            col.Item().Table(table =>
        //            {
        //                table.ColumnsDefinition(columns =>
        //                {
        //                    columns.RelativeColumn(2); // Precio Unitario
        //                    columns.RelativeColumn(2); // Subtotal
        //                });

        //                // HEADER AZUL
        //                table.Header(header =>
        //                {
        //                    header.Cell().Element(CellStyle).Background(Colors.Blue.Medium).AlignCenter().Text("Correo").FontColor(Colors.White).SemiBold();
        //                    header.Cell().Element(CellStyle).Background(Colors.Blue.Medium).AlignCenter().Text("Celular").FontColor(Colors.White).SemiBold();

        //                    static IContainer CellStyle(IContainer container) =>
        //                        container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).PaddingHorizontal(2);
        //                });

        //                table.Cell().AlignCenter().Text($"{_data.ClientEmail}");
        //                table.Cell().AlignCenter().Text($"3001234567");

        //            });

        //            col.Item().PaddingTop(20);

        //            // DETALLE PRODUCTOS
        //            col.Item().PaddingTop(15).Text("Detalle de la compra").FontSize(16).Bold();

        //            col.Item().Table(table =>
        //            {
        //                table.ColumnsDefinition(columns =>
        //                {
        //                    columns.RelativeColumn(3); // Producto
        //                    columns.RelativeColumn(1); // Cantidad
        //                    columns.RelativeColumn(2); // Precio Unitario
        //                    columns.RelativeColumn(2); // Subtotal
        //                });

        //                // HEADER AZUL
        //                table.Header(header =>
        //                {
        //                    header.Cell().Element(CellStyle).Background(Colors.Blue.Medium).Text("Producto").FontColor(Colors.White).SemiBold();
        //                    header.Cell().Element(CellStyle).Background(Colors.Blue.Medium).AlignCenter().Text("Cantidad").FontColor(Colors.White).SemiBold();
        //                    header.Cell().Element(CellStyle).Background(Colors.Blue.Medium).AlignRight().Text("Precio Unitario").FontColor(Colors.White).SemiBold();
        //                    header.Cell().Element(CellStyle).Background(Colors.Blue.Medium).AlignRight().Text("Subtotal").FontColor(Colors.White).SemiBold();

        //                    static IContainer CellStyle(IContainer container) =>
        //                        container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).PaddingHorizontal(2);
        //                });

        //                foreach (var item in _data.Items)
        //                {
        //                    table.Cell().Text(item.ProductName);
        //                    table.Cell().AlignCenter().Text(item.Quantity.ToString());
        //                    table.Cell().AlignRight().Text($"{item.UnitPrice:C}");
        //                    table.Cell().AlignRight().Text($"{item.Subtotal:C}");
        //                }
        //            });

        //            col.Item().PaddingTop(20);

        //            col.Item().AlignRight().Background(Colors.Grey.Lighten3).Padding(10).Text(text =>
        //            {
        //                text.Span("Iva: ").Bold();
        //                text.Span($"19%");
        //                text.Span($"{_data.TotalAmount:C}").FontColor(Colors.Green.Darken2).Bold();
        //            });
        //            col.Item().AlignRight().Background(Colors.Grey.Lighten3).Padding(10).Text(text =>
        //            {
        //                text.Span("Total: ").Bold();
        //                text.Span($"   ").Bold();
        //                text.Span($"{_data.TotalAmount:C}").FontColor(Colors.Green.Darken2).Bold();
        //            });
        //        });

        //        // FOOTER
        //        page.Footer().Column(col =>
        //        {

        //            col.Item().AlignCenter().PaddingTop(10).Column(info =>
        //            {
        //                info.Item().Text("Mi Empresa S.A.").FontSize(8);
        //                info.Item().Text("Dirección: Calle Ficticia 123").FontSize(8);
        //                info.Item().Text("Tel: 300 123 4567").FontSize(8);
        //            });
        //        });
        //    });
        //}

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(12));

                // HEADER
                page.Header().Row(row =>
                {
                    row.RelativeColumn()
                        .Column(col =>
                        {
                            col.Item().Text("Factura de Venta").FontSize(20).Bold();
                            col.Item().Text($"N° Factura: {_data.InvoiceNumber}");
                            col.Item().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}");
                        });

                    row.ConstantColumn(200)
                        .AlignRight()
                        .Column(col =>
                        {
                            col.Item().Text($"{_datosEmpresa.Nombre}").FontSize(20).Bold();
                        });
                });

                // CLIENTE
                page.Content().PaddingVertical(10).Column(col =>
                {

                    col.Item().PaddingTop(15).Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten3)
                    .Padding(10)
                    .Row(row =>
                    {
                        // Lado izquierdo: Datos del Cliente
                        row.RelativeColumn()
                            .Column(innerCol =>
                            {
                                innerCol.Item().Text("Datos del Cliente")?.FontSize(14).Bold();
                                innerCol.Item().Text(" ");
                                innerCol.Item().Text($"{_data.ClientName}");
                                innerCol.Item().Text($"{_data.ClientDocument}");
                                innerCol.Item().Text($"{_data.ClientEmail}");
                                innerCol.Item().Text($"{_data.ClientPhone}");
                            });

                        // Línea divisoria vertical
                        row.ConstantColumn(10)
                        .BorderLeft(1)
                        .BorderColor(Colors.Grey.Lighten1)
                        .PaddingHorizontal(5);

                        // Lado derecho: Datos de la Empresa
                        row.RelativeColumn()
                            .Column(innerCol =>
                            {
                                innerCol.Item().Text("Datos de la Empresa")?.FontSize(14).Bold();
                                innerCol.Item().Text(" ");
                                innerCol.Item().Text($"{_datosEmpresa.Nombre}");
                                innerCol.Item().Text($"{_datosEmpresa.Nit}");
                                innerCol.Item().Text($"{_datosEmpresa.Direccion}");
                                innerCol.Item().Text($"{_datosEmpresa.Celular}");
                            });
                    });

                    col.Item().PaddingTop(20);

                    // DETALLE PRODUCTOS
                    col.Item().PaddingTop(15).Text("Detalle de la compra").FontSize(16).Bold();

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // Producto
                            columns.RelativeColumn(1); // Cantidad
                            columns.RelativeColumn(2); // Precio Unitario
                            columns.RelativeColumn(2); // Subtotal
                        });

                        // HEADER AZUL
                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Background(Colors.Blue.Medium).Text("Producto").FontColor(Colors.White).SemiBold();
                            header.Cell().Element(CellStyle).Background(Colors.Blue.Medium).AlignCenter().Text("Cantidad").FontColor(Colors.White).SemiBold();
                            header.Cell().Element(CellStyle).Background(Colors.Blue.Medium).AlignRight().Text("Precio Unitario").FontColor(Colors.White).SemiBold();
                            header.Cell().Element(CellStyle).Background(Colors.Blue.Medium).AlignRight().Text("Subtotal").FontColor(Colors.White).SemiBold();

                            static IContainer CellStyle(IContainer container) =>
                                container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).PaddingHorizontal(2);
                        });

                        foreach (var item in _data.Items)
                        {
                            table.Cell().Text(item.ProductName);
                            table.Cell().AlignCenter().Text(item.Quantity.ToString());
                            table.Cell().AlignRight().Text($"{item.UnitPrice:C}");
                            table.Cell().AlignRight().Text($"{item.Subtotal:C}");
                        }
                    });

                    col.Item().PaddingTop(20);

                    col.Item().AlignLeft().Column(inner =>
                    {
                        // Bloque IVA
                        inner.Item().Width(230)
                            .Background(Colors.White)
                            .Padding(10)
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(3);
                                });

                                table.Cell().Element(CellStyle).Text("").Bold();
                                table.Cell().Element(CellStyle).AlignLeft().Text("").Bold();
                                table.Cell().Element(CellStyle).AlignLeft().Text("").Bold();
                            });

                    });

                    col.Item().Row(row =>
                    {
                        // Columna izquierda: Forma de Pago
                        row.ConstantColumn(230).Background(Colors.White).Padding(10).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            table.Cell().Element(CellStyle).Text("Forma de Pago:").Bold();
                            table.Cell().Element(CellStyle).Text($"{_data.PaymentMethod}"); // o como sea que lo quieras mostrar
                        });

                        // Espacio opcional entre columnas
                        row.RelativeColumn();

                        // Columna derecha: Totales
                        row.ConstantColumn(230).AlignRight().Column(inner =>
                        {
                            // Bloque IVA
                            inner.Item().Background(Colors.Grey.Lighten3).Padding(10).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(3);
                                });

                                table.Cell().Element(CellStyle).Text("Iva:").Bold();
                                table.Cell().Element(CellStyle).AlignRight().Text("19%").Bold();
                                table.Cell().Element(CellStyle).AlignRight().Text($"{_data.TotalIva:C}").Bold();
                            });

                            inner.Item().Height(5);

                            // Bloque Total
                            inner.Item().Background(Colors.Grey.Lighten3).Padding(10).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(3);
                                });

                                table.Cell().Element(CellStyle).Text("Total:").Bold();
                                table.Cell().Element(CellStyle).AlignRight().Text("").Bold();
                                table.Cell().Element(CellStyle).AlignRight().Text($"{_data.TotalAmount:C}").FontColor(Colors.Green.Darken2).Bold();
                            });
                        });
                    });
                    static IContainer CellStyle(IContainer container) =>
                    container.PaddingVertical(2).PaddingHorizontal(5);
                });

                // FOOTER
                page.Footer().Column(col =>
                {
                    col.Item().PaddingTop(5).AlignCenter().Text($"{_datosEmpresa.Nombre}").FontSize(8);
                    col.Item().PaddingTop(5).AlignCenter().Text($"{_datosEmpresa.Direccion}").FontSize(8);
                    col.Item().PaddingTop(5).AlignCenter().Text($"{_datosEmpresa.Correo}").FontSize(8);
                    col.Item().PaddingTop(5).AlignCenter().Text($"{_datosEmpresa.Celular}").FontSize(8);

                    //col.Item().AlignCenter().PaddingTop(10).Column(info =>
                    //{
                    //    info.Item().AlignCenter().Text($"{_datosEmpresa.Nombre}").FontSize(8);
                    //    info.Item().AlignCenter().Text($"{_datosEmpresa.Direccion}").FontSize(8);
                    //    info.Item().AlignCenter().Text($"{_datosEmpresa.Correo}").FontSize(8);
                    //    info.Item().AlignCenter().Text($"{_datosEmpresa.Celular}").FontSize(8);
                    //});
                });
            });
        }


    }

}
