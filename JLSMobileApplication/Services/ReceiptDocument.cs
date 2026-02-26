using System;
using System.Linq;
using JLSApplicationBackend.HtmlToPdf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JLSApplicationBackend.Services;

public class ReceiptDocument : IDocument
{
    private readonly ReceiptInfo _model;
    private readonly string _title;

    public ReceiptDocument(ReceiptInfo model, string title)
    {
        _model = model;
        _title = title;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(_title).FontSize(24).ExtraBold().FontColor(Colors.Blue.Medium).AlignCenter();
                
                column.Item().PaddingTop(10).Row(r => {
                    r.RelativeItem().Text($"Numéro de commande : {_model.OrderId}");
                    r.RelativeItem().AlignRight().Text($"Date de transaction : {_model.CreatedOn:dd/MM/yyyy HH:mm:ss}");
                });
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(20).Column(column =>
        {
            // Customer Info Table
            column.Item().Text("Info client").FontSize(14).SemiBold();
            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Entreprise");
                    header.Cell().Element(CellStyle).Text("Numéro de téléphone");
                    header.Cell().Element(CellStyle).Text("Compte");
                    header.Cell().Element(CellStyle).Text("N° TVA");

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                    }
                });

                table.Cell().Element(ContentStyle).Text(_model.Entreprise);
                table.Cell().Element(ContentStyle).Text(_model.PhoneNumber);
                table.Cell().Element(ContentStyle).Text(_model.Username);
                table.Cell().Element(ContentStyle).Text(_model.Siret);

                static IContainer ContentStyle(IContainer container)
                {
                    return container.PaddingVertical(5);
                }
            });

            // Products Table
            column.Item().PaddingTop(20).Text("Produits").FontSize(14).SemiBold();
            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(50); // Photo
                    columns.RelativeColumn();   // Reference
                    columns.RelativeColumn(2); // Name
                    columns.RelativeColumn();   // Colisage
                    columns.RelativeColumn();   // Colis
                    columns.RelativeColumn();   // Qty
                    columns.RelativeColumn();   // P.U. HT
                    columns.RelativeColumn();   // Total HT
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Photo");
                    header.Cell().Element(CellStyle).Text("Référence");
                    header.Cell().Element(CellStyle).Text("Nom de produit");
                    header.Cell().Element(CellStyle).Text("Colisage");
                    header.Cell().Element(CellStyle).Text("Nombre de colis");
                    header.Cell().Element(CellStyle).Text("QT CDEE");
                    header.Cell().Element(CellStyle).Text("P.U. HT");
                    header.Cell().Element(CellStyle).Text("Montant HT");

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                    }
                });

                foreach (var item in _model.ProductList)
                {
                    var totalQty = item.Quantity * item.Colissage;
                    var itemTotalPrice = (item.Price * item.Quantity * item.Colissage).ToString("0.00");
                    
                    var isModified = item.IsModifiedPriceOrBox == true;

                    // Photo
                    table.Cell().PaddingBottom(2).MaxHeight(40).AlignCenter().Element(e => {
                        if (!string.IsNullOrEmpty(item.PhotoPath))
                        {
                            try{ e.Image(item.PhotoPath); } catch { e.Text("N/A"); }
                        }
                        return e;
                    });
                    
                    table.Cell().Element(c => ContentStyle(c, isModified)).Text(item.Code);
                    table.Cell().Element(c => ContentStyle(c, isModified)).Text(item.Label);
                    
                    table.Cell().Element(c => ContentStyle(c, isModified)).Row(r => {
                        r.RelativeItem().Text(x => {
                            if (item.QuantityPerParcel != null) x.Span(item.Colissage.ToString()).FontColor(Colors.Red.Medium);
                            else x.Span(item.Colissage.ToString());
                            
                            if (item.QuantityPerParcel != null) x.Span($" ({item.QuantityPerParcel})");
                        });
                    });
                    
                    table.Cell().Element(c => ContentStyle(c, isModified)).Text(item.Quantity.ToString());
                    table.Cell().Element(c => ContentStyle(c, isModified)).Text(totalQty.ToString());
                    table.Cell().Element(c => ContentStyle(c, isModified)).Text($"{item.Price} €");
                    table.Cell().Element(c => ContentStyle(c, isModified)).Text($"{itemTotalPrice} €");
                }

                static IContainer ContentStyle(IContainer container, bool isRed)
                {
                    var content = container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                    return isRed ? content.DefaultTextStyle(x => x.FontColor(Colors.Red.Medium)) : content;
                }
            });

            // Summary
            column.Item().AlignRight().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(150);
                    columns.ConstantColumn(100);
                });

                table.Cell().Text("Total HT").SemiBold();
                table.Cell().AlignRight().Text($"{_model.TotalPriceWithoutTax:0.00} €");

                table.Cell().Text($"Tx TVA ({_model.TaxRate}%)").SemiBold();
                table.Cell().AlignRight().Text($"{_model.Tax:0.00} €");

                table.Cell().PaddingTop(5).Text("TOTAL A PAYER").FontSize(12).ExtraBold();
                table.Cell().PaddingTop(5).AlignRight().Text($"{_model.TotalPrice:0.00} €").FontSize(12).ExtraBold();
            });

            // Addresses
            column.Item().PaddingTop(30).Row(row =>
            {
                row.RelativeItem().Column(coll =>
                {
                    coll.Item().Text("Addresse de facturation").FontSize(11).SemiBold();
                    coll.Item().PaddingTop(5).Element(e => ComposeAddress(e, _model.FacturationAddress));
                });

                row.ConstantItem(20);

                row.RelativeItem().Column(coll =>
                {
                    coll.Item().Text("Addresse de livraison").FontSize(11).SemiBold();
                    coll.Item().PaddingTop(5).Element(e => ComposeAddress(e, _model.ShipmentAddress));
                });
            });

            // Client Remark
            if (!string.IsNullOrEmpty(_model.ClientRemark))
            {
                column.Item().PaddingTop(20).Column(c =>
                {
                    c.Item().Text("Message de client").FontSize(11).SemiBold();
                    c.Item().PaddingTop(5).Border(1).Padding(5).Text(_model.ClientRemark);
                });
            }
        });
    }

    private void ComposeAddress(IContainer container, JLSDataModel.Models.Adress.Adress address)
    {
        container.Column(column =>
        {
            column.Item().Text(_model.Entreprise);
            column.Item().Text(address.FirstLineAddress);
            if (!string.IsNullOrEmpty(address.SecondLineAddress)) column.Item().Text(address.SecondLineAddress);
            column.Item().Text($"{address.ZipCode}, {address.City}, {address.Country}");
            column.Item().Text($"Numéro de téléphone: {address.ContactTelephone}");
        });
    }
}
