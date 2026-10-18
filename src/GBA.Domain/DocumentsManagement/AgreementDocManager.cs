using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Helpers.WordHelpers;
using NPOI.OpenXmlFormats.Wordprocessing;
using NPOI.XWPF.UserModel;

namespace GBA.Domain.DocumentsManagement;

public sealed class AgreementDocManager : BaseXlsManager, IAgreementDocManager {
    public string ExportWarrantyConditionsToDoc(
        string path,
        ClientAgreement clientAgreement,
        IEnumerable<DocumentMonth> months) {
        DateTime current = DateTime.Now;
        Agreement agreement = clientAgreement.Agreement;
        Organization organization = clientAgreement.Agreement.Organization;
        Client client = clientAgreement.Client;

        string fileName = Path.Combine(path, $"{agreement.Number}_{Guid.NewGuid()}.docx");

        if (File.Exists(fileName)) File.Delete(fileName);

        using FileStream stream = new(fileName, FileMode.Create, FileAccess.Write);
        WordDocument document = new();

        document.SetDefaultStyle(StyleBuilder.GetDefaultStyle());

        document.SetDocumentMargin(1700, 935, 850, 1008);

        document.AddStyle(new StyleBuilder().IsBold(true).Build("Bold"));
        document.AddStyle(new StyleBuilder().SetFirstLineIndentationInChars(360).Build("FirstLineTab"));
        document.AddStyle(new StyleBuilder().IsBold(true).IsItalic(true).SetFontSize(12).Build("Tab1"));
        document.AddStyle(new StyleBuilder().SetFontSize(10).SetIndentationInPoint(100, 100).SetLineSpacing(-230).Build("BasicCellStyle"));

        CT_SectPr secPr = document.Document.Document.body.sectPr;

        //Create footer and set its text
        CT_Ftr footer = new();
        CT_P ctP = footer.AddNewP();
        CT_R ctR = ctP.AddNewR();

        // Dynamic numbering
        CT_SimpleField m_fldSimple = new();
        m_fldSimple.instr = " PAGE  \\* Arabic  \\* MERGEFORMAT ";
        ctP.Items.Add(m_fldSimple);
        ctP.AddNewPPr().jc = new CT_Jc { val = ST_Jc.center };

        //Create the relation of footer
        XWPFRelation relation = XWPFRelation.FOOTER;
        XWPFFooter myFooter = (XWPFFooter)document.Document.CreateRelationship(relation, XWPFFactory.GetInstance(), document.Document.FooterList.Count + 1);

        //Set the footer
        myFooter.SetHeaderFooter(footer);
        CT_HdrFtrRef myFooterRef = secPr.AddNewFooterReference();
        myFooterRef.type = ST_HdrFtr.@default;
        myFooterRef.id = myFooter.GetPackageRelationship().Id;

        DateTime from = agreement.FromDate ?? current;

        document.AddParagraph(ParagraphAlignment.RIGHT)
            .SetStyleId("Bold")
            .AppendText(
                "Додаток 1",
                $"до договору № {agreement.Number}",
                $"від «{from.Day}» {months.FirstOrDefault(m => m.Number.Equals(from.Month))?.Name.ToLower() ?? string.Empty} {from.Year} року")
            .AddCarriageReturn();

        document.AddParagraph(ParagraphAlignment.CENTER)
            .SetStyleId("Bold")
            .AppendText("ГАРАНТІЙНІ УМОВИ",
                "ТОВ «АМГ «КОНКОРД»");

        document.AddParagraph()
            .SetStyleId("FirstLineTab")
            .SetText(
                "У Додатку 1 встановлені гарантійні умови на запасні частини, приладдя, деталі (далі - " +
                "Товар), придбані Покупцями у ТОВ «АМГ «КОНКОРД» (далі - Продавець), які визначаються " +
                "виробником Товарів та регулюються Публічним договором купівлі-продажу запасних частин в " +
                "інтернет-магазині, або окремим договором, укладеним з Продавцем, а також Порядком дій " +
                "Покупця та Продавця при настанні гарантійного випадку та Особливими умовами гарантії, які " +
                "визначають особливості гарантійної підтримки на певні групи товару певних виробників. ")
            .AddCarriageReturn();

        WordList wordList = document.CreateList();

        wordList.AddListItem("0")
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetStyleId("Bold")
            .SetText("ТЕРМІНИ, ЩО ВЖИВАЮТЬСЯ В ГАРАНТІЙНИХ УМОВАХ");

        // Start 1.1

        wordList.AddListItem("1")
            .SetText(
                "Гарантійний термін – це період, протягом якого Продавець бере на себе зобов’язання здійснення безкоштовного ремонту, " +
                "заміни Товару або повернення Покупцю коштів у випадку настання гарантійної події.");

        wordList.AddListItem("1")
            .SetText("Продавець – суб'єкт господарювання, який згідно з договором реалізує споживачеві товари або пропонує їх до реалізації.");

        wordList.AddListItem("1")
            .SetText("Гарантійний випадок – випадок, з яким законодавство пов’язує вжиття Продавцем заходів щодо безоплатного усунення " +
                     "недоліків/дефектів в Товарах, здійснення безкоштовного ремонту Товарів або повернення Покупцю коштів, або вжиття інших заходів передбачених законодавством.")
            .AddCarriageReturn();

        // End 1.3

        wordList.AddListItem("0")
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetStyleId("Bold")
            .SetText("УМОВИ ПОВЕРНЕННЯ ТОВАРІВ НАЛЕЖНОЇ ЯКОСТІ");

        // Start 2.1

        wordList.AddListItem("1")
            .SetText(
                "Відповідно до положень Закону України «Про захист прав споживачів» Покупець, який придбав товар належної якості," +
                " має право повернути його Продавцю протягом 14 (чотирнадцяти) днів з дати придбання.");

        wordList.AddListItem("1")
            .SetText("Вимоги до Товару, який повертається Покупцем:");

        wordList.AddListItem("2")
            .SetText("Товар не має ознак встановлення та/або використання;");

        wordList.AddListItem("2")
            .SetText("Товар знаходиться в оригінальній упаковці виробника (якщо упаковка передбачена виробником) в тому самому вигляді, в якому його було придбано;");

        wordList.AddListItem("1")
            .SetText(
                "Разом з Товаром надаються всі супровідні документи на нього (технічна документація, гарантійний талон, тощо)" +
                " та документи, за якими Товар був отриманий Покупцем із зазначенням штрих-коду з номером замовлення;");

        wordList.AddListItem("1")
            .SetText("Погодження повернення Товару здійснюється через особистий кабінет Покупця в інтернет-магазині " +
                     "Продавця або безпосередньо за адресою складу Продавця із зазначенням причини повернення.");

        wordList.AddListItem("1")
            .SetText($"Адресою складу Продавця є: {organization.Address}");

        wordList.AddListItem("1")
            .SetText("При поверненні Товарів перевізником, Покупець повідомляє Продавцю номер вантажної декларації " +
                     "та найменування перевізника в особистому кабінеті Покупця в інтернет-магазині Продавця в день відправлення Товару")
            .AddCarriageReturn();

        // END 2.6

        wordList.AddListItem("0")
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetStyleId("Bold")
            .SetText("ТОВАРИ НАЛЕЖНОЇ ЯКОСТІ, ЯКІ НЕ ПІДЛЯГАЮТЬ ПОВЕРНЕННЮ");

        // Start 3.1

        wordList.AddListItem("1")
            .SetText("Не підлягають поверненню Товари належної якості, які:");

        wordList.AddListItem("2")
            .SetText("Не мають індивідуальної упаковки (були продані в роздріб з однієї упаковки);");

        wordList.AddListItem("2")
            .SetText("Не відповідають умовам, за яких їх може бути повернуто.")
            .AddCarriageReturn();

        // End 3.1.2

        wordList.AddListItem("0")
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetStyleId("Bold")
            .SetText("ГАРАНТІЙНИЙ ТЕРМІН");

        // 4.1.

        wordList.AddListItem("1")
            .SetText("Продавець забезпечує нормальну роботу (застосування, використання) Товару, у тому числі комплектуючих виробів, протягом гарантійного терміну.");

        wordList.AddListItem("1")
            .SetText(
                "Гарантійний термін на Товар становить 6-12 (шість-дванадцять) місяців (в залежності від умов виробника) " +
                "з дати придбання товару, якщо інше не передбачено особливими умовами гарантії.");

        wordList.AddListItem("1")
            .SetText("Особливі умови гарантії визначені у Таблиці 1.");

        wordList.AddListItem("1")
            .SetText("Гарантійний термін обраховується з моменту придбання Товару Покупцем. У разі здійснення продажу товарів з " +
                     "пересиланням поштою, а також у разі, коли час укладення договору і час передачі Товару Покупцеві не збігаються, " +
                     "гарантійний термін обчислюється від дня доставки Товару Покупцеві.");

        wordList.AddListItem("1")
            .SetText("Покупець зобов’язаний перевірити відсутність дефектів при отриманні Товару.")
            .AddCarriageReturn();

        // END 4.5

        wordList.AddListItem("0")
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetStyleId("Bold")
            .SetText("ВИПАДКИ, НА ЯКІ НЕ ПОШИРЮЄТЬСЯ ГАРАНТІЯ");

        wordList.AddListItem("1")
            .SetText("Гарантія на Товар (запасні частини) не поширюється в наступних випадках: ");

        wordList.AddListItem("2")
            .SetText("Нормального зносу запасної частини. Перелік деталей, схильних до природнього / нормального зносу: ");

        // 5.1.1. - sublist start

        wordList.AddListItem("3")
            .SetText("лампи фар, ліхтарів і плафонів, запобіжники;");

        wordList.AddListItem("3")
            .SetText("щітки склоочисників, скла кузова;");

        wordList.AddListItem("3")
            .SetText("форсунки омивача вітрового і заднього скла;");

        wordList.AddListItem("3")
            .SetText("фільтри, гальмівні накладки, гальмівні колодки, гальмівні диски; ");

        wordList.AddListItem("3")
            .SetText("гальмівні барабани; ");

        wordList.AddListItem("3")
            .SetText("диски фрикційні і кошики зчеплення; ");

        wordList.AddListItem("3")
            .SetText("провідний диск і ведений диск зчеплення; ");

        wordList.AddListItem("3")
            .SetText("високовольтні дроти свічок запалювання; ");

        wordList.AddListItem("3")
            .SetText("свічки запалювання / розжарювання; ");

        wordList.AddListItem("3")
            .SetText("шланги і патрубки; ");

        wordList.AddListItem("3")
            .SetText("приводні ремені навісних агрегатів двигуна; ");

        wordList.AddListItem("3")
            .SetText("гумові захисні чохли і втулки;");

        wordList.AddListItem("3")
            .SetText("елементи системи випуску відпрацьованих газів; ");

        wordList.AddListItem("3")
            .SetText("механізми, приводи скло підйомників і дзеркал; ");

        wordList.AddListItem("3")
            .SetText("хромування декоративних елементів кузова та інтер'єру, ЛКП дисків коліс; ");

        wordList.AddListItem("3")
            .SetText("деталі оздоблення салону; ");

        wordList.AddListItem("3")
            .SetText("щітки стартера і генератора; ");

        wordList.AddListItem("3")
            .SetText("діодний міст генератора; ");

        wordList.AddListItem("3")
            .SetText("заправні рідини і мастильні матеріали; ");

        wordList.AddListItem("3")
            .SetText("ресори.");

        // 5.1.1. - sublist end

        wordList.AddListItem("2")
            .SetText("Пошкодження Товару в результаті ДТП або недбалої експлуатації. ");

        wordList.AddListItem("2")
            .SetText("Несправності запасних частин паливної системи і системи випуску внаслідок застосування неякісного палива " +
                     "(в тому числі через забруднення або застосування етильованого бензину).");

        wordList.AddListItem("2")
            .SetText(
                "Пошкодження (в тому числі підвіски і кермового управління), що виникли через неакуратне керування на нерівностях доріг, сполученого з ударними навантаженнями на автомобіль.");

        wordList.AddListItem("2")
            .SetText("Шум (скрип, писк) гальм.");

        wordList.AddListItem("2")
            .SetText("Зовнішні пошкодження приладів освітлення. ");

        wordList.AddListItem("2")
            .SetText(
                "Дефекти, несправності або корозія Товару, що виникли в результаті впливу промислових і хімічних викидів, " +
                "кислотного або лужного забруднення повітря, рослинного соку, продуктів життєдіяльності птахів і тварин, хімічно активних речовин, " +
                "в тому числі застосовуваних для боротьби з обмерзанням доріг, граду, блискавки і інших природних явищ. ");

        wordList.AddListItem("2")
            .SetText(
                "Експлуатаційного зносу і природної зміни стану (в тому числі старіння) таких запасних частин як приводні ремені, " +
                "гальмівні колодки, диски і барабани, диски зчеплення, свічки запалювання і т.д.");

        wordList.AddListItem("2")
            .SetText("Якщо у замовленні-наряді на установку запасної частини на автомобіль НЕ вказані (НЕ проведені) " +
                     "обов'язкові супутні роботи, пов'язані в тому числі і з обов'язковою заміною інших запасних частин, без проведення яких встановлювана запасна частина може вийти з ладу. ");

        wordList.AddListItem("2")
            .SetText("Якщо Покупцем порушені основні правила експлуатації і обслуговування автомобіля:");

        //5.1.10 - sublist start

        wordList.AddListItem("3")
            .SetText("Несвоєчасне проведення періодичного обслуговування (як по пробігу, так і за часовим проміжком) в повному обсязі.");

        wordList.AddListItem("3")
            .SetText("Проведення обслуговування (ремонту) або установка оригінальних деталей не у офіційного дилера. ");

        wordList.AddListItem("3")
            .SetText("Використання неякісних паливно-мастильних матеріалів. ");

        wordList.AddListItem("3")
            .SetText("Недотримання Інструкції по експлуатації. ");

        wordList.AddListItem("3")
            .SetText("Проведення модифікації, модернізації компонентів автомобіля або окремих агрегатів не у офіційного дилера або без його офіційного дозволу.");

        //5.1.10 - sublist end

        wordList.AddListItem("1")
            .SetText("Експлуатації автомобіля в тяжких умовах, може бути причиною обмеження терміну дії гарантії або відмови в гарантійному обслуговуванні.");

        wordList.AddListItem("1")
            .SetText("У разі виявлення навмисної зміни показань спідометра автомобіля гарантія на деталі, встановлені на даний автомобіль, не поширюється. ");

        wordList.AddListItem("1")
            .SetText("У разі не виконання вимог, приписів, зазначених у гарантійному талоні виробника запасних частин, гарантія на деталі не поширюється. ")
            .AddCarriageReturn();

        // END 5.4.

        wordList.AddListItem("0")
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetStyleId("Bold")
            .AppendText("ПОРЯДОК ДІЙ ПРОДАВЦЯ ТА ПОКУПЦЯ")
            .AddCarriageReturn()
            .AppendText("ПРИ НАСТАННІ ГАРАНТІЙНОГО ВИПАДКУ");

        // Start 6.1.

        //TODO make hyperlink
        wordList.AddListItem("1")
            .SetText("Покупець, не пізніше ніж через 7 (сім) днів, з моменту виявлення недоліку товару, в особистому кабінеті Покупця на сайті " +
                     "http://concord-shop.com/ повинен сформувати Акт рекламації та зазначити в ньому всю необхідну інформацію відповідно до п.7 цих гарантійних умов.");

        wordList.AddListItem("1")
            .SetText("Разом із Актом рекламації в особистому кабінеті Покупця, останнім додаються наступні документи: ");

        wordList.AddListItem("2")
            .SetText("скан-копія наряду-замовлення (згідно якого товар встановлювався на автомобіль);");

        wordList.AddListItem("2")
            .SetText("файли із фото або відео-записом неналежної роботи товару; ");

        wordList.AddListItem("2")
            .SetText("протокол з діагностичного приладу з помилкою (для електронних приладів). ");

        wordList.AddListItem("1")
            .SetText(
                "Продавець розглядає надіслане через особистий кабінет звернення Покупця та дає відповідь не пізніше, ніж через 7 (сім) днів з моменту отримання такого звернення.");

        wordList.AddListItem("1")
            .SetText("В разі, якщо Продавець дійшов висновку, що рекламаційне звернення підлягає гарантійному розгляду, " +
                     $"Покупець повинен відправити дефектний товар, разом зі всіма необхідними документами за адресою: {organization.Address}");

        wordList.AddListItem("1")
            .SetText("Товар надсилається Покупцем разом із документами та в упаковці, яка забезпечує його транспортування та зберігання.");

        wordList.AddListItem("1")
            .SetText(
                "Не пізніше, ніж через 7 (сім) днів з моменту отримання Товару неналежної якості, Продавець " +
                "робить повторний огляд товару та відправляє відповідне звернення до виробника про виявлений дефект товару.");

        wordList.AddListItem("1")
            .SetText("Період розгляду звернення виробником, може тривати від 10 (десяти) до 30 (тридцяти) днів, в залежності від Товару та специфіки дефекту.");

        wordList.AddListItem("1")
            .SetText(
                "Виробник, також може запросити від Продавця, відправити дефектний товару до найближчого відділення виробника, для детальної діагностики та перевірки. " +
                "В цьому разі період  рекламаційного розгляду може бути збільшений.");

        wordList.AddListItem("1")
            .SetText("Не пізніше, ніж через 5 днів після отримання відповіді від виробника, Продавець має повідомити Покупця про висновок " +
                     "та чи був дефект спричинений з вини виробника чи з вини недбалої експлуатації Покупцем.");

        wordList.AddListItem("1")
            .SetText("У випадку, якщо дефект товару спричинений з вини виробника, Продавець зобов’язується вжити заходів, " +
                     "передбачених чинним законодавством у випадку продажу товару неналежної якості (повернення коштів, заміна товару, ремонт тощо).");

        wordList.AddListItem("1")
            .SetText("У випадку, якщо згідно висновку виробника спричинення дефекту в разі недбалої експлуатації Покупцем, Продавець має право відмовити " +
                     "у компенсації вартості дефектного Товару. Дефектний Товар в такому разі підлягає поверненню Покупцю.")
            .AddCarriageReturn();

        // END 6.11

        wordList.AddListItem("0")
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetStyleId("Bold")
            .SetText("ПОРЯДОК ЗАПОВНЕННЯ АКТУ РЕКЛАМАЦІЇ");

        // 7.1.

        wordList.AddListItem("1")
            .SetText("В Акті рекламації в обов’язковому порядку зазначаються:");

        wordList.AddListItem("2")
            .SetText("оригінальний код товару та найменування виробника;");

        wordList.AddListItem("2")
            .SetText("дата покупки Товару;");

        wordList.AddListItem("2")
            .SetText("кількість Товару, що надається на рекламаційний розгляд.");

        wordList.AddListItem("2")
            .SetText("марка, модель, номер кузову, рік виготовлення, пробіг транспортного засобу (на момент монтажу/демонтажу), на якому було встановлено товар;");

        wordList.AddListItem("2")
            .SetText("дата монтажу/демонтажу товару;");

        wordList.AddListItem("2")
            .SetText("обставини та характер виявленого недоліку товару.");

        wordList.AddListItem("1")
            .SetText(
                "У разі відсутності заповненого Акту рекламацій на сайті чи його друкованої версії разом з присланим дефектним товаром, рекламація не буде підлягати розгляду Продавцем. ");

        wordList.AddListItem("1")
            .SetText("У разі необхідності у Покупця може бути запрошена додаткова інформація або документи від СТО, де проводилася установка виробу.")
            .AddCarriageReturn();

        // END 7.3.

        document.AddParagraph(ParagraphAlignment.RIGHT)
            .SetStyleId("Tab1")
            .SetText("Таблиця 1");

        document.AddParagraph(ParagraphAlignment.CENTER)
            .SetStyleId("Bold")
            .SetText("ОСОБЛИВІ УМОВИ ГАРАНТІЇ")
            .AddCarriageReturn()
            .Run.FontSize = 12;

        WordTable table = document.CreateTable(9, 5);

        // Header Row
        table.BuildCell(table.GetCell(0, 0))
            .SetCurrentCellWidth("6")
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetText("№")
            .Run.IsBold = true;

        table.BuildCell(table.GetCell(0, 1))
            .SetCurrentCellWidth("20")
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetText("Назва товару")
            .Run.IsBold = true;

        table.BuildCell(table.GetCell(0, 2))
            .SetCurrentCellWidth("18.5")
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetText("Гарантійний строк")
            .Run.IsBold = true;

        table.BuildCell(table.GetCell(0, 3))
            .SetCurrentCellWidth("14.9")
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetText("Обрахування строку")
            .Run.IsBold = true;

        table.BuildCell(table.GetCell(0, 4))
            .SetCurrentCellWidth("40.6")
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetText("Примітка")
            .Run.IsBold = true;

        // 1TH Row
        table.BuildCell(table.GetCell(1, 0))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("1.");

        table.BuildCell(table.GetCell(1, 1))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Гальмівні диски і колодки");

        table.BuildCell(table.GetCell(1, 2))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("1 місяць або 10 000 (одна тисяча) кілометрів пробігу");

        table.BuildCell(table.GetCell(1, 3))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("З моменту придбання Товару Покупцем/ дня доставки Товару Покупцеві");

        table.BuildCell(table.GetCell(1, 4))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("На комерційну групи");

        // 2TH row
        table.BuildCell(table.GetCell(2, 0))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("2.");

        table.BuildCell(table.GetCell(2, 1))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Амортизатори");

        table.BuildCell(table.GetCell(2, 2))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("6 місяців, але не більше 20 000 (двадцять тисяч) кілометрів пробігу ");

        table.BuildCell(table.GetCell(2, 3))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("З моменту придбання Товару Покупцем/ дня доставки Товару Покупцеві");

        table.BuildCell(table.GetCell(2, 4))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText(
                "За умови: 1) заміни пари амортизаторів на " +
                "одну вісь автомобіля; 2) пиловики, " +
                "відбійники, пружини і опори " +
                "амортизаторів на момент установки були " +
                "у справному стані. Якщо амортизатор не " +
                "має візуальних дефектів (слідів робочої " +
                "рідини, ослаблення або обриву " +
                "сайлентблоків, осьової люфтштока і т.д.), " +
                "об'єктивною оцінкою його стану може " +
                "служити тільки технічний звіт-роздруківка з діагностичного вібро-" +
                "стенду, яка є підставою для подачі рекламації.");

        // 3TH Row
        table.BuildCell(table.GetCell(3, 0))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("3.");

        table.BuildCell(table.GetCell(3, 1))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Радіатори, системи охолодження ");

        table.BuildCell(table.GetCell(3, 2))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("6 місяців або 20 000 (двадцять тисяч) кілометрів пробігу");

        table.BuildCell(table.GetCell(3, 3))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("З моменту придбання Товару Покупцем/ дня доставки Товару Покупцеві");

        table.BuildCell(table.GetCell(3, 4))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("На комерційну групи");

        // 4TH Row
        table.BuildCell(table.GetCell(4, 0))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("4.");

        table.BuildCell(table.GetCell(4, 1))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Важелі, тяги, кермові наконечники, ШРУСи, гумометалічні вироби, підшипники ступиці ");

        table.BuildCell(table.GetCell(4, 2))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("6 місяців, але не більше 20 000 (двадцять тисяч) кілометрів пробігу");

        table.BuildCell(table.GetCell(4, 3))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("З моменту придбання Товару Покупцем/ дня доставки Товару Покупцеві");

        table.BuildCell(table.GetCell(4, 4))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText(
                "За умови дотримання технології установки. Не розглядаються як заводський брак деталі підвіски, які були в експлуатації з пошкодженими пильовиками шарнірів.");

        // 5TH Row
        table.BuildCell(table.GetCell(5, 0))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("5.");

        table.BuildCell(table.GetCell(5, 1))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Деталі електрообладнання (лампи, котушки і дроти запалювання, датчики, свічки запалювання, розжарення, лямбда-зонди) ");

        table.BuildCell(table.GetCell(5, 2))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("");

        table.BuildCell(table.GetCell(5, 3))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("З моменту придбання Товару Покупцем/ дня доставки Товару Покупцеві");

        table.BuildCell(table.GetCell(5, 4))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText(
                "Гарантійний термін залежить від: 1) справності проводки автомобіля, реле, запобіжників, блоків управління, тощо; 2) стану і правильно підібраних свічок запалювання, датчика положення колінчастого валу, електронного блоку управління та ін.. Для підтвердження рекламаційного випадку необхідно надати розгорнутий звіт діагностики. При діагностиці не схваленими виробником діагностичними тестерами (універсальними) необхідно надати інтерпретацію отриманого коду несправності. Деталі, що відносяться до електроустаткування, перед установкою необхідно перевіряти на стенді. Після установки деталі до повернення не приймаються.");

        // 6TH Row
        table.BuildCell(table.GetCell(6, 0))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("6.");

        table.BuildCell(table.GetCell(6, 1))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Комплекти ГРМ  ");

        table.BuildCell(table.GetCell(6, 2))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("6 місяців");

        table.BuildCell(table.GetCell(6, 3))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("З моменту придбання Товару Покупцем/ дня доставки Товару Покупцеві");

        table.BuildCell(table.GetCell(6, 4))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("На комерційну групи");

        // 7TH Row
        table.BuildCell(table.GetCell(7, 0))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("7.");

        table.BuildCell(table.GetCell(7, 1))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Натяжні, обвідні ролики, помпи й демпфери, обгонні муфти, насоси ГУР");

        table.BuildCell(table.GetCell(7, 2))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("6 місяців або 20 000 (двадцять тисяч) кілометрів пробігу");

        table.BuildCell(table.GetCell(7, 3))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("З моменту придбання Товару Покупцем/ дня доставки Товару Покупцеві");

        table.BuildCell(table.GetCell(7, 4))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("За умови дотримання технології установки і використання робочих рідин, рекомендованих виробником автомобіля (у випадку з установкою насоса ГУР).");

        // 8TH Row
        table.BuildCell(table.GetCell(8, 0))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("8.");

        table.BuildCell(table.GetCell(8, 1))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Комплекти зчеплення, двомасових маховиків ");

        table.BuildCell(table.GetCell(8, 2))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("6 місяців або 20 000 (двадцять тисяч) кілометрів пробігу");

        table.BuildCell(table.GetCell(8, 3))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("З моменту придбання Товару Покупцем/ дня доставки Товару Покупцеві");

        table.BuildCell(table.GetCell(8, 4))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText(
                "Не розглядаються по гарантії комплекти зчеплення і маховики, які піддавалися високим ударним і температурним навантаженням, про які свідчитимуть сліди перегріву металу, тріщини або обриви демпферів, «злизування» шліцьової частини диска зчеплення, тощо.");

        table.SetCellsStyle("BasicCellStyle");


        document.AddParagraph(ParagraphAlignment.CENTER);

        document.AddParagraph(ParagraphAlignment.CENTER)
            .SetStyleId("Bold")
            .SetText("ПІДПИСИ ТА РЕКВІЗИТИ СТОРІН");

        WordTable tableRequisites = document.CreateTable(4, 2);
        tableRequisites.RemoveBorders();

        XWPFRun leftHeaderRun = tableRequisites.BuildCell(tableRequisites.GetCell(0, 0))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .SetCurrentCellWidth("50")
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetText("ПРОДАВЕЦЬ")
            .Run;

        leftHeaderRun.IsBold = true;
        leftHeaderRun.FontSize = 12;
        leftHeaderRun.Paragraph.SpacingAfter = 200;

        XWPFRun rightHeaderRun = tableRequisites.BuildCell(tableRequisites.GetCell(0, 1))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .SetCurrentCellWidth("50")
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetText("ПОКУПЕЦЬ")
            .Run;

        rightHeaderRun.IsBold = true;
        rightHeaderRun.FontSize = 12;
        rightHeaderRun.Paragraph.SpacingAfter = 200;

        tableRequisites.BuildCell(tableRequisites.GetCell(1, 0))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetText(organization.FullName)
            .AddBreak()
            .Run.IsBold = true;

        tableRequisites.BuildCell(tableRequisites.GetCell(1, 1))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetText(client.FullName)
            .AddBreak()
            .Run.IsBold = true;

        string accountNumber = string.Empty;
        string bankName = string.Empty;

        if (organization.MainPaymentRegister != null) {
            accountNumber = !string.IsNullOrEmpty(organization.MainPaymentRegister.AccountNumber)
                ? $"П/р {organization.MainPaymentRegister.AccountNumber}, "
                : string.Empty;
            bankName = !string.IsNullOrEmpty(organization.MainPaymentRegister.BankName)
                ? $"Банк {organization.MainPaymentRegister.BankName} "
                : string.Empty;
        }

        tableRequisites.BuildCell(tableRequisites.GetCell(2, 0))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .AppendText(!string.IsNullOrEmpty(organization.Address) ? $"Адреса: {organization.Address}" : string.Empty)
            .AddBreak()
            .AppendText(!string.IsNullOrEmpty(organization.USREOU) ? $"КОД ЄДРПОУ {organization.USREOU}" : string.Empty)
            .AddBreak()
            .AppendText(!string.IsNullOrEmpty(organization.TIN) ? $"ІПН {organization.TIN}" : string.Empty)
            .AddBreak()
            .AppendText(accountNumber)
            .AddBreak()
            .AppendText(bankName)
            .AddBreak();

        tableRequisites.BuildCell(tableRequisites.GetCell(2, 1))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .AppendText(!string.IsNullOrEmpty(client.LegalAddress) ? $"Адреса: {client.LegalAddress} " : string.Empty)
            .AddBreak()
            .AppendText(!string.IsNullOrEmpty(client.USREOU) ? $"КОД ЄДРПОУ {client.USREOU} " : string.Empty)
            .AddBreak()
            .AppendText(!string.IsNullOrEmpty(client.TIN) ? $"ІПН {client.TIN}" : string.Empty)
            .AddBreak();

        string organizationManager = !string.IsNullOrEmpty(organization.Manager) ? organization.Manager : "______________";
        string clientManager = !string.IsNullOrEmpty(client.Manager) ? client.Manager : "______________";

        tableRequisites.BuildCell(tableRequisites.GetCell(3, 0))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.BOTTOM)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .SetText($"____________________/{organizationManager}/")
            .AddBreak()
            .AppendText("    м. п./підпис");

        tableRequisites.BuildCell(tableRequisites.GetCell(3, 1))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.BOTTOM)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .SetText($"____________________/{clientManager}/")
            .AddBreak()
            .AppendText("    м. п./підпис");


        tableRequisites.SetCellsStyle("BasicCellStyle");

        document.Write(stream);

        return fileName;
    }

    public string ExportAgreementToDoc(
        string path,
        ClientAgreement clientAgreement,
        IEnumerable<DocumentMonth> months) {
        DateTime current = DateTime.Now;

        Agreement agreement = clientAgreement.Agreement;
        Organization organization = clientAgreement.Agreement.Organization;
        Client client = clientAgreement.Client;

        string fileName = Path.Combine(path, $"{agreement.Number}_{Guid.NewGuid()}.docx");

        using FileStream stream = new(fileName, FileMode.Create, FileAccess.Write);
        WordDocument document = new();

        // TODO fix bottom margin
        document.SetDocumentMargin(1265, 935, 420, 1);

        document.SetDefaultStyle(new StyleBuilder()
            .SetFont("Times New Roman")
            .SetFontSize(10)
            .NoProof(true)
            .SetLineSpacing(-227)
            .SetSpaceAfter(1)
            .SetSpaceBefore(1)
            .SetIsDefault(true)
            .Build("Default"));

        document.AddStyle(new StyleBuilder().IsBold(true).Build("Bold"));
        document.AddStyle(new StyleBuilder().IsBold(true).SetFontSize(20).SetLineSpacing(-330).Build("Title"));
        document.AddStyle(new StyleBuilder().SetFirstLineIndentationInChars(360).Build("FirstLineTab"));
        document.AddStyle(new StyleBuilder().IsBold(true).IsItalic(true).SetFontSize(12).Build("Tab1"));
        document.AddStyle(new StyleBuilder().SetFontSize(10).SetIndentationInPoint(100, 100).SetLineSpacing(-230).Build("BasicCellStyle"));
        document.AddStyle(new StyleBuilder().IsBold(true).SetFirstLineIndentationInChars(220).Build("MainListLvl"));

        if (document.Document.Document.body.sectPr == null) document.Document.Document.body.sectPr = new CT_SectPr();

        CT_SectPr secPr = document.Document.Document.body.sectPr;

        //Create footer and set its text
        CT_Ftr footer = new();
        CT_P ctP = footer.AddNewP();

        ctP.AddNewR().AddNewT().Value =
            "__________________________                                                                                     " +
            "__________________________      ";

        // Dynamic numbering
        CT_SimpleField m_fldSimple = new();
        m_fldSimple.instr = " PAGE  \\* Arabic  \\* MERGEFORMAT ";
        ctP.Items.Add(m_fldSimple);

        CT_P footerTextCtP = footer.AddNewP();
        CT_R ctR = footerTextCtP.AddNewR();
        CT_RPr footerRPr = ctR.AddNewRPr();
        footerRPr.sz = new CT_HpsMeasure { val = 18 };
        footerRPr.i = new CT_OnOff { val = true };
        // ctR.AddNewRPr().bdr = new CT_Border {val = ST_Border.starsTop, sz = 10};

        ctR.AddNewT().Value = "     Від ПОСТАЧАЛЬНИКА                                                                            " +
                              "                                             Від ПОКУПЦЯ";

        //Create the relation of footer
        XWPFRelation relation = XWPFRelation.FOOTER;
        XWPFFooter myFooter = (XWPFFooter)document.Document.CreateRelationship(relation, XWPFFactory.GetInstance(), document.Document.FooterList.Count + 1);

        //Set the footer
        myFooter.SetHeaderFooter(footer);
        CT_HdrFtrRef myFooterRef = secPr.AddNewFooterReference();
        myFooterRef.type = ST_HdrFtr.@default;
        myFooterRef.id = myFooter.GetPackageRelationship().Id;

        XWPFParagraph titleParagraph = document.AddParagraph(ParagraphAlignment.CENTER)
            .SetStyleId("Title")
            .AppendText("Договір поставки товару № ")
            .Paragraph;

        XWPFRun titleRun = titleParagraph.CreateRun();
        titleRun.Underline = UnderlinePatterns.Single;
        titleRun.AppendText(agreement.Number);
        titleRun.AddCarriageReturn();

        document.AddParagraph();

        DateTime from = agreement.FromDate ?? current;

        document.AddParagraph()
            .SetStyleId("FirstLineTab")
            .SetText("м Хмельницький                                                                                                       " +
                     $"«{from.Day}» {months.FirstOrDefault(m => m.Number.Equals(from.Month))?.Name.ToLower() ?? string.Empty} {from.Year} р.")
            .AddCarriageReturn()
            .Run.IsBold = true;

        document.AddParagraph()
            .SetStyleId("FirstLineTab")
            .SetText(string.Format(
                "\"{0}\" (далі «ПОСТАЧАЛЬНИК»)" +
                ", що є платником податку на прибуток на загальних умовах оподаткування, в особі директора {1}, " +
                "який діє на підставі Статуту  з однієї сторони, та " +
                "{2}" +
                " (надалі іменується \"ПОКУПЕЦЬ\") в особі директора " +
                "{3}" +
                ", що діє {4} з другої  сторони, (в подальшому разом іменуються \"Сторони\"" +
                ", а кожна окремо – \"Сторона\") уклали цей Договір поставки товару (надалі іменується \"Договір\") про наступне.",
                clientAgreement.Agreement.Organization.FullName,
                clientAgreement.Agreement.Organization.Manager,
                clientAgreement.Client.FullName,
                clientAgreement.Client.Manager,
                client.IsIndividual
                    ? "як суб’єкт господарювання від свого власного імені, на підставі Виписки з Єдиного державного реєстру юридичних осіб, " +
                      "фізичних осіб-підприємців та громадських формувань"
                    : "на підставі Статуту"))
            .AddCarriageReturn();

        WordList wordList = document.CreateList();

        XWPFNumbering numbering = document.Document.CreateNumbering();

        CT_AbstractNum ct_abn = new();
        CT_MultiLevelType mlt = new();
        mlt.val = ST_MultiLevelType.multilevel;
        ct_abn.multiLevelType = mlt;
        ct_abn.lvl = new List<CT_Lvl> {
            new() {
                ilvl = "0", start = new CT_DecimalNumber { val = "1" }, numFmt = new CT_NumFmt { val = ST_NumberFormat.@decimal },
                lvlText = new CT_LevelText { val = "%1." }, lvlJc = new CT_Jc { val = ST_Jc.left },
                rPr = new CT_RPr { b = new CT_OnOff { val = true } },
                pPr = new CT_PPr {
                    ind = new CT_Ind {
                        firstLineChars = "360",
                        leftChars = "360"
                    },
                    rPr = new CT_ParaRPr {
                        b = new CT_OnOff { val = true }
                    }
                }
            },
            new() {
                ilvl = "1", start = new CT_DecimalNumber { val = "1" }, numFmt = new CT_NumFmt { val = ST_NumberFormat.@decimal },
                lvlText = new CT_LevelText { val = "%1.%2." }, lvlJc = new CT_Jc { val = ST_Jc.left },
                pPr = new CT_PPr { ind = new CT_Ind { firstLineChars = "360" }, jc = new CT_Jc { val = ST_Jc.both } }
            },
            new() {
                ilvl = "2", start = new CT_DecimalNumber { val = "1" }, numFmt = new CT_NumFmt { val = ST_NumberFormat.@decimal },
                lvlText = new CT_LevelText { val = "%1.%2.%3." }, lvlJc = new CT_Jc { val = ST_Jc.left },
                pPr = new CT_PPr { ind = new CT_Ind { firstLineChars = "360" } }
            },
            new() {
                ilvl = "3", numFmt = new CT_NumFmt { val = ST_NumberFormat.none },
                lvlText = new CT_LevelText { val = "" }, lvlJc = new CT_Jc { val = ST_Jc.left }, // TODO Figure out how to put "" in list
                pPr = new CT_PPr {
                    ind = new CT_Ind { firstLineChars = "360" },
                    rPr = new CT_ParaRPr {
                        rFonts = new CT_Fonts {
                            ascii = "Wingdings",
                            cs = "Wingdings",
                            eastAsia = "Wingdings",
                            hAnsi = "Wingdings",
                            hint = ST_Hint.@default
                        }
                    }
                }
            }
        };

        string abstractNumId = numbering.AddAbstractNum(new XWPFAbstractNum(ct_abn));
        string numId = numbering.AddNum(abstractNumId);

        wordList.SetNewNumbering(numId);

        wordList.AddListItem("0")
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .SetStyleId("MainListLvl")
            .SetText("ЗАГАЛЬНІ ПОЛОЖЕННЯ");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("ПОСТАЧАЛЬНИК зобов'язується поставити ПОКУПЦЮ Товар - Запчастини на умовах, " +
                     "передбачених цим договором, а ПОКУПЕЦЬ зобов'язується прийняти та оплатити визначений цим " +
                     "Договором товар.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Найменування, асортимент, кількість, ціна та загальна вартість партій товару, " +
                     "що поставляється за даним Договором, визначається у видаткових накладних та/або рахунках " +
                     "на оплату та/або замовленнях Покупця та\\або інших документах, які після підписання їх сторонами, " +
                     "мають юридичну силу специфікацій та є невід’ємною частиною Договору.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Умови цього Договору викладені Сторонами у відповідності до " +
                     "вимог Міжнародних правил щодо тлумачення термінів \"Інкотермс\" (в редакції 2020 року), які " +
                     "застосовуються із урахуванням   особливостей, пов'язаних із внутрішньодержавним характером цього " +
                     "Договору, а також тих особливостей, що випливають із умов цього Договору.")
            .AddCarriageReturn();

        wordList.AddListItem("0")
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .SetStyleId("MainListLvl")
            .SetText("СУМА ДОГОВОРУ ТА ПОРЯДОК РОЗРАХУНКІВ");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Загальна сума Договору  визначається як сума вартості всіх поставлених партій товару за цим Договором.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Оплата за товар  здійснюється ПОКУПЦЕМ  шляхом безготівкового переказу " +
                     "коштів на поточний рахунок ПОСТАЧАЛЬНИКА, який вказаний у реквізитах ПОСТАЧАЛЬНИКА в " +
                     "національній валюті України, протягом 3-х календарних днів з дати отримання товару ПОКУПЦЕМ " +
                     "згідно видаткової накладної, крім випадків передбачених пп.2.3, 2.6. Договору.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("На вимогу ПОСТАЧАЛЬНИКА, оплата за товар по цьому Договору здійснюється " +
                     "у формі попередньої оплати про що Сторони зазначають в рахунку на оплату та призначенні платежу");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Ціни на товари, що постачаються ПОСТАЧАЛЬНИКОМ, є звичайними вільними відпускними цінами " +
                     "та визначаються по взаємному погодженню сторін на кожну окрему поставку у видаткових " +
                     "накладних, рахунках на оплату. Сторони визначають, що ціни на товари, погоджені сторонами, " +
                     "є попередніми і можуть змінюватися ПОСТАЧАЛЬНИКОМ в залежності від показників, які обумовлюють " +
                     "вартість товару (собівартість, витрати) протягом строку дії Договору. Ціни на товар встановлюються на кожну партію окремо");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("При здійсненні платежу ПОКУПЕЦЬ обов’язково повинен вказувати у " +
                     "платіжному дорученні номер та дату рахунку на оплату.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("В разі порушення з вини ПОКУПЦЯ умов здійснення розрахунків, передбачених п.п. 2.2. - 2.3., 2.5. " +
                     "даного Договору, ПОСТАЧАЛЬНИК має право надалі припинити поставки товарів ПОКУПЦЮ на " +
                     "умовах відстрочення платежу, і вимагати від ПОКУПЦЯ часткової чи повної попередньої оплати " +
                     "за товар.  Передбачений цим пунктом порядок здійснення розрахунків за поставлені товари " +
                     "застосовується Сторонами до моменту повного погашення ПОКУПЦЕМ заборгованості за поставлений товар.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Покупець отримує товар за ціною що вказана у видатковій накладній та/або рахунку на оплату.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Датою оплати є дата зарахування грошових коштів на поточний рахунок ПОСТАЧАЛЬНИКА.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Не рідше одного разу  на рік Сторони проводять звірку взаєморозрахунків з обов’язковим підписанням " +
                     "Акту звірки. ПОКУПЕЦЬ направляє ПОСТАЧАЛЬНИКУ Акт звірки шляхом передачі його засобами " +
                     "факсимільного зв’язку або електронною поштою. У випадку не згоди з Актом звірки, ПОСТАЧАЛЬНИК " +
                     "протягом 10 (десяти) робочих днів з моменту отримання Акту звірки, зобов’язаний направити ПОКУПЦЮ " +
                     "вмотивовану відмову від підписання Акту звірки  з вказанням всіх заперечень.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Сторони домовились, що ПОСТАЧАЛЬНИК має право утримувати в першочерговому порядку " +
                     "із сум, сплачених ПОКУПЦЕМ по цьому договору: в першу чергу – витрати ПОСТАЧАЛЬНИКА, " +
                     "пов’язані з одержанням оплати товару від ПОКУПЦЯ, в другу чергу - суму пені, процентів, " +
                     "інфляційних, в третю чергу – суму вартості товару, в четверту чергу – інші витрати, платежі.")
            .AddCarriageReturn();

        wordList.AddListItem("0")
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .SetStyleId("MainListLvl")
            .SetText("АСОРТИМЕНТ ТОВАРУ");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Асортимент товару, що є предметом поставки за цим " +
                     "Договором, зазначається у видаткових накладних та/або рахунках на оплату.")
            .AddCarriageReturn();

        wordList.AddListItem("0")
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .SetStyleId("MainListLvl")
            .SetText("ЯКІСТЬ ТА КОМПЛЕКТНІСТЬ ТОВАРУ");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Товар має відповідати вимогам чинних нормативних документів. Якість товару " +
                     "може підтверджуватись відповідними сертифікатами, свідоцтвами, тощо згідно діючого законодавства.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("За вимогами ПОКУПЦЯ ПОСТАЧАЛЬНИК надає сертифікати якості на кожну партію товару.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Кількість Товару, що поставляється, вказується у відповідних видаткових накладних на кожну партію товару.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Приймання товару по кількості та якості проводиться ПОКУПЦЕМ в момент його отримання " +
                     "від ПОСТАЧАЛЬНИКА. ПОКУПЕЦЬ зобов’язаний перевірити комплектність, цілісність тари, пломб " +
                     "на ній (при їх наявності), а також відсутність ознак пошкодження товару і, у випадку їх виявлення, " +
                     "негайно, письмово заявити про це ПОСТАЧАЛЬНИКУ. При відсутності такої заяви товар вважається прийнятим " +
                     "ПОКУПЦЕМ по кількості та якості. Підтвердженням прийому товару є підпис уповноваженого представника " +
                     "ПОКУПЦЯ у видатковій накладній на отримання товарно-матеріальних цінностей.")
            .AddCarriageReturn();

        wordList.AddListItem("0")
            .SetStyleId("MainListLvl")
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .SetText("СТРОК ТА ПОРЯДОК ПОСТАВКИ");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("ПОСТАЧАЛЬНИК передає ПОКУПЦЮ товар окремими партіями на умовах самовивозу " +
                     "EXW - склад ПОСТАЧАЛЬНИКА в м. Хмельницький або через спеціалізовані служби перевезення " +
                     "за домовленістю сторін в узгоджені строки.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Датою передачі (придбання) товару є дата виписки видаткової накладної. " +
                     "Підпис представника ПОКУПЦЯ на видатковій накладній є погодженням ПОКУПЦЯ " +
                     "асортименту, кількості, ціни, вартості товару, який постачається за цим Договором.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Право власності від ПОСТАЧАЛЬНИКА до ПОКУПЦЯ переходить з моменту підписання видаткових  накладних.")
            .AddCarriageReturn();

        wordList.AddListItem("0")
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .SetStyleId("MainListLvl")
            .SetText("ВІДПОВІДАЛЬНІСТЬ СТОРІН ЗА ПОРУШЕННЯ ДОГОВОРУ");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("У випадку порушення зобов'язання, що виникає з цього Договору (надалі іменується " +
                     "\"порушення Договору\"), Сторона несе відповідальність, визначену цим Договором та (або) " +
                     "чинним в Україні законодавством.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("В разі прострочки поставки товару, ПОСТАЧАЛЬНИК сплачує ПОКУПЦЮ за кожний день прострочки " +
                     "пеню в розмірі подвійної облікової ставки Національного банку України від суми простроченої поставки.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("В разі прострочки оплати товару, ПОКУПЕЦЬ сплачує ПОСТАЧАЛЬНИКУ за кожний день " +
                     "прострочки пеню в розмірі подвійної облікової ставки Національного банку України від суми простроченого платежу.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("За прострочення виконання зобов’язань по оплаті товару, ПОКУПЕЦЬ зобов’язаний сплатити  " +
                     "ПОСТАЧАЛЬНИКУ відповідно до  ст. 625 Цивільного кодексу України суму боргу з урахуванням встановленого " +
                     "індексу інфляції за весь час прострочення та тридцять процентів річних з простроченої суми.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Суми ПДВ, зазначені в зареєстрованих у ЄРПН податкових накладних, які ПОСТАЧАЛЬНИК видає ПОКУПЦЮ, " +
                     "мають відображатися в податкових зобов’язаннях декларації з ПДВ ПОСТАЧАЛЬНИКУ в тому звітному періоді, " +
                     "у якому були виписані податкові накладні. ПОСТАЧАЛЬНИК зобов’язаний подати в податкові органи декларації " +
                     "з ПДВ у встановлений законом строк.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("У разі, якщо під час перевірки Державною податковою службою України операцій з Товаром буде " +
                     "встановлено недійність цього Договору в цілому, або окремих його частин, а також у разі виявлення розбіжностей між " +
                     "даними ПОКУПЦЯ і ПОСТАЧАЛЬНИКА в Єдиному реєстрі податкових накладних після надання звітності за " +
                     "підсумками періоду, у якому відбулися поставки за цим Договором, винна сторона зобов’язується компенсувати іншій " +
                     "стороні суму всіх коригувань (в тому числі: ПДВ), зроблених такою стороною у разі, якщо ці коригування були " +
                     "зроблені через некоректно надану винною стороною звітність в податкову інспекцію або виявлені порушення " +
                     "операцій з Товаром в процесі податкової перевірки, а також компенсувати суму можливих фінансових штрафних " +
                     "санкцій. Додатково до винної сторони може застосовуватися штраф у розмірі 20 % від загальної вартості Товару, який " +
                     "винна сторона зобов’язана сплатити протягом трьох банківських днів з дати отримання письмової вимоги від іншої " +
                     "сторони.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("В разі, якщо ПОКУПЦЮ анулюють реєстрацію платника податку на додану вартість, або " +
                     "ПОКУПЕЦЬ зареєструвався платником податку на додану вартість, ПОКУПЕЦЬ зобов’язаний письмово " +
                     "повідомити про це ПОСТАЧАЛЬНИКА негайно, протягом трьох робочих днів з дати анулювання, або " +
                     "реєстрації платником податку на додану вартість. У разі порушення цих вимог, ПОСТАЧАЛЬНИК має " +
                     "право, на стягнення з ПОКУПЦЯ штрафу в розмірі 20% від загальної вартості Товару, який ПОКУПЕЦЬ " +
                     "зобов’язаний сплатити протягом трьох банківських днів з дати отримання письмової вимоги від ПОСТАЧАЛЬНИКА.")
            .AddCarriageReturn();

        wordList.AddListItem("0")
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .SetStyleId("MainListLvl")
            .SetText("ФОРС-МАЖОРНІ ОБСТАВИНИ");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("У випадку настання обставин, що знаходяться поза контролем сторiн ( форс- мажорних ) , " +
                     "якi сторонами не могли бути передбаченi i врахованi, як то : стихiйнi лиха, технiчнi аварії, " +
                     "вiйськовi дii,  забастовки  (крiм персоналу сторiн ), дiї властей, затримки в постачанні товару " +
                     "виробником (постачальником), митному оформленні товару - сторона, що пiдпала під  дiю таких обставин," +
                     " звiльняється  вiд  вiдповiдальностi за невиконання зобов’язань по Договору при умові підтвердження  " +
                     "факту існування форс-мажорних обставин довідкою територіального органу Торгово-Промислової Палати України.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Існування форс-мажорних обставин звільняє сторону, яка підпала під  їх дію, від відповідальності " +
                     "за невиконання зобов’язань по цьому договору , але не звільняє від обов’язку  виконати свої зобов’язання.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Якщо у зв'язку із форс-мажорними обставинами та (або) їх наслідками, за які жодна " +
                     "із Сторін не відповідає, виконання цього Договору є остаточно неможливим, то цей Договір " +
                     "вважається припиненим з моменту виникнення неможливості виконання цього Договору.")
            .AddCarriageReturn();

        wordList.AddListItem("0")
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .SetStyleId("MainListLvl")
            .SetText("ВИРІШЕННЯ СПОРІВ");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Усі спори, що виникають з цього Договору або пов'язані із ним, вирішуються шляхом переговорів між Сторонами.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Якщо відповідний спір неможливо вирішити шляхом переговорів, " +
                     "він вирішується в судовому порядку за встановленою підвідомчістю та підсудністю такого спору " +
                     "відповідно до чинного в Україні законодавства")
            .AddCarriageReturn();

        wordList.AddListItem("0")
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .SetStyleId("MainListLvl")
            .SetText("ДІЯ ДОГОВОРУ");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Цей Договір вважається укладеним і набирає чинності з " +
                     "моменту його підписання Сторонами та скріплення печатками Сторін.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Договір набирає чинності з дати його укладення (підписання) сторонами і діє " +
                     $"до {agreement.ToDate.Value.Day} {months.FirstOrDefault(m => m.Number.Equals(agreement.ToDate.Value.Month))?.Name.ToLower() ?? string.Empty} {agreement.ToDate.Value.Year} р. , але у будь-якому випадку не раніше повного та належного виконання " +
                     "Сторонами своїх зобов'язань за даним Договором");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Закінчення строку цього Договору не звільняє Сторони від відповідальності за " +
                     "його порушення, яке мало місце під час дії цього Договору.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Сторони погодили, що у випадку, якщо за один місяць до закінчення строку дії цього " +
                     "Договору жодна із Сторін не подала письмову заяву про його припинення чи внесення змін, " +
                     "то Договір вважається продовженим на той самий строк і тих самих умовах. " +
                     "Положення цього пункту поширюються на кожне наступне продовження дії договору необмежену кількість раз.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Якщо інше прямо не передбачено цим Договором або чинним в Україні законодавством, зміни " +
                     "у цей Договір можуть бути внесені тільки за домовленістю Сторін, яка оформлюється додатковою " +
                     "угодою до цього Договору.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Якщо інше прямо не передбачено цим Договором або чинним в Україні законодавством, цей " +
                     "Договір може бути розірваний тільки за домовленістю Сторін, яка оформлюється додатковою " +
                     "угодою до цього Договору.")
            .AddCarriageReturn();

        wordList.AddListItem("0")
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .SetStyleId("MainListLvl")
            .SetText("ПРИКІНЦЕВІ ПОЛОЖЕННЯ");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Усі правовідносини, що виникають з цього Договору або пов'язані із ним, у тому числі " +
                     "пов'язані із дійсністю, укладенням, виконанням, зміною та припиненням цього Договору, " +
                     "тлумаченням його умов, визначенням наслідків недійсності або порушення Договору, регламентуються " +
                     "цим Договором та відповідними нормами чинного в Україні законодавства, а також застосовними до " +
                     "таких правовідносин звичаями ділового обороту на підставі принципів добросовісності, розумності " +
                     "та справедливості.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Після підписання цього Договору всі попередні переговори за ним, листування, попередні " +
                     "договори, протоколи про наміри та будь-які інші усні або письмові домовленості Сторін " +
                     "з питань, що так чи інакше стосуються цього Договору, втрачають юридичну силу, але " +
                     "можуть братися до уваги при тлумаченні умов цього Договору.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Сторони несуть повну відповідальність за правильність вказаних ними у цьому Договорі " +
                     "реквізитів та зобов‘язуються своєчасно у письмовій формі повідомляти іншу Сторону про " +
                     "їх зміну, а у разі неповідомлення несуть ризик настання пов'язаних із ним несприятливих наслідків.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Відступлення права вимоги та (або) переведення боргу за цим Договором однією із Сторін " +
                     "до третіх осіб допускається виключно за умови письмового погодження цього із іншою Стороною.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Додаткові угоди та додатки до цього Договору є його невід'ємними частинами і мають " +
                     "юридичну силу у разі, якщо вони викладені у письмовій формі, підписані Сторонами та " +
                     "скріплені їх печатками.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Всі виправлення за текстом цього Договору мають силу та можуть братися до уваги виключно " +
                     "за умови, що вони у кожному окремому випадку датовані, засвідчені підписами Сторін " +
                     "та скріплені їх печатками.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Цей Договір складений при повному розумінні Сторонами його умов та термінології " +
                     "українською мовою, у двох автентичних примірниках, які мають однакову юридичну силу, – " +
                     "по одному для кожної із Сторін.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("При підписанні даного договору Сторони надають одна одній копії " +
                     "документів, засвідчених своєю печаткою, а  саме:");

        wordList.AddListItem("3")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("свідоцтво (виписку) про державну реєстрацію");

        wordList.AddListItem("3")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("свідоцтво платника податку");

        wordList.AddListItem("3")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("документ, який підтверджує повноваження особи яка уклала договір");

        wordList.AddListItem("3")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("довідка ЄДРПОУ");

        wordList.AddListItem("3")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("паспорт та ідентифікаційний код (для фізичних-осіб підприємців)");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Копії підписаного та скріпленого печаткою Сторонами Договору та/або доповнень, змін " +
                     "до нього, а також копії документів, складених на виконання цього договору, надіслані за " +
                     "допомогою факсимільного зв'язку та/або електронною поштою та/або з допомогою мобільних додатків " +
                     "месенджерів (Viber, WhatsApp, Telegram) мають юридичну силу до моменту обміну оригіналами таких документів.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Сторони, у зв’язку з цим Договором, передають одна одній персональні дані своїх представників або " +
                     "інших осіб - суб’єктів персональних даних. Сторона, що передає персональні дані, гарантує, що а) вона " +
                     "є законним та правомірним володільцем відповідних(ої) баз(и) персональних даних в розумінні Закону України " +
                     "«Про захист персональних даних» (надалі – Закон), б) вона отримала згоду на обробку та передачу " +
                     "персональних даних, що передаються іншій Стороні, від відповідних суб’єктів персональних даних, та в) " +
                     "передача персональних даних здійснюється із дотриманням вимог чинного законодавства України в сфері " +
                     "захисту персональних даних та мети обробки персональних даних. Сторона, що отримує персональні дані " +
                     "від іншої Сторони, є третьою особою в розумінні Закону. Сторона, що отримала персональні дані відповідно " +
                     "до умов цього Договору, обробляє такі персональні дані виключно у зв’язку з цим Договором. Сторони " +
                     "забезпечують всі необхідні організаційні та технічні засоби для належного захисту отриманих персональних " +
                     "даних від несанкціонованого доступу або обробки. У разі порушення однією із Сторін вимог законодавства " +
                     "про захист персональних даних, інша Сторона не несе відповідальності за таке порушення. Сторони " +
                     "зобов’язуються відшкодувати одна одній будь-які збитки та витрати, пов’язані із розглядом або задоволенням " +
                     "будь-яких претензій з боку суб’єктів, чиї персональні дані передаються відповідно до цього розділу " +
                     "Договору та чиї права були порушені через невиконання відповідною Стороною зобов’язань, передбачених " +
                     "цим розділом Договору, а також інші витрати або збитки. У випадку відкликання суб’єктом персональних " +
                     "даних своєї згоди на обробку переданих персональних даних у базі даних однієї із Сторін, така " +
                     "Сторона зобов’язана повідомити іншу Сторону про строк та умови припинення обробки персональних " +
                     "даних такого суб’єкта.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Сторони гарантують, заявляють та зобов’язуються одна перед одною, що при виконанні цього " +
                     "Договору вони дотримуватимуться всіх належних законів, правил та норм, включно, зокрема, з " +
                     "санкціями, законами з протидії корупції та легалізації доходів, отриманих незаконним шляхом, " +
                     "а також податкового законодавства. У звязку з цим, усі ризики покладаються на Сторони з вини " +
                     "якої настали несприятливі наслідки.");

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText(string.Format("Сторони домовилися, що відповідальним представником щодо виконання даного Договору з " +
                                   "боку ПОСТАЧАЛЬНИКА є – {0} (контактний телефон {1} ), " +
                                   "а відповідальним представником щодо виконання Договору з боку Покупця є – " +
                                   "директор {2}  (контактний телефон {3} ).",
                organization.Manager,
                string.IsNullOrEmpty(organization.PhoneNumber) ? "—" : organization.PhoneNumber,
                string.IsNullOrEmpty(client.Manager) ? "—" : client.Manager,
                string.IsNullOrEmpty(client.DirectorNumber) ? "—" : client.DirectorNumber));

        wordList.AddListItem("1")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Сторони домовилися, що вся електронна переписка по умовам даного Договору " +
                     "ведеться шляхом надсилання електронних листів на електронну адресу Сторін.");

        wordList.AddListItem("2")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText("Електронна адреса відповідального представника ПОСТАЧАЛЬНИКА - Sale@concord.km.ua");

        wordList.AddListItem("2")
            .SetParagraphAlignment(ParagraphAlignment.BOTH)
            .SetText(string.Format("Електронна адреса відповідального представника Покупця - {0}. ",
                string.IsNullOrEmpty(client.EmailAddress) ? "—" : client.EmailAddress))
            .AddCarriageReturn();

        wordList.AddListItem("0")
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .SetStyleId("MainListLvl")
            .SetText("МІСЦЕЗНАХОДЖЕННЯ ТА РЕКВІЗИТИ СТОРІН")
            .AddCarriageReturn();

        WordTable tableRequisites = document.CreateTable(4, 2);
        tableRequisites.RemoveBorders();

        XWPFRun leftHeaderRun = tableRequisites.BuildCell(tableRequisites.GetCell(0, 0))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .SetCurrentCellWidth("50")
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetText("ПРОДАВЕЦЬ")
            .Run;

        leftHeaderRun.IsBold = true;
        leftHeaderRun.FontSize = 12;
        leftHeaderRun.Paragraph.SpacingAfter = 200;

        XWPFRun rightHeaderRun = tableRequisites.BuildCell(tableRequisites.GetCell(0, 1))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .SetCurrentCellWidth("50")
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetText("ПОКУПЕЦЬ")
            .Run;

        rightHeaderRun.IsBold = true;
        rightHeaderRun.FontSize = 12;
        rightHeaderRun.Paragraph.SpacingAfter = 200;

        tableRequisites.BuildCell(tableRequisites.GetCell(1, 0))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetText(organization.FullName)
            .AddBreak()
            .Run.IsBold = true;

        tableRequisites.BuildCell(tableRequisites.GetCell(1, 1))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.CENTER)
            .SetText(client.FullName)
            .AddBreak()
            .Run.IsBold = true;

        string accountNumber = string.Empty;
        string bankName = string.Empty;

        if (organization.MainPaymentRegister != null) {
            accountNumber = !string.IsNullOrEmpty(organization.MainPaymentRegister.AccountNumber)
                ? $"П/р {organization.MainPaymentRegister.AccountNumber}, "
                : string.Empty;
            bankName = !string.IsNullOrEmpty(organization.MainPaymentRegister.BankName)
                ? $"Банк {organization.MainPaymentRegister.BankName} "
                : string.Empty;
        }

        tableRequisites.BuildCell(tableRequisites.GetCell(2, 0))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .AppendText(!string.IsNullOrEmpty(organization.Address) ? $"Адреса: {organization.Address}" : string.Empty)
            .AddBreak()
            .AppendText(!string.IsNullOrEmpty(organization.USREOU) ? $"КОД ЄДРПОУ {organization.USREOU}" : string.Empty)
            .AddBreak()
            .AppendText(!string.IsNullOrEmpty(organization.TIN) ? $"ІПН {organization.TIN}" : string.Empty)
            .AddBreak()
            .AppendText(accountNumber)
            .AddBreak()
            .AppendText(bankName)
            .AddBreak();

        tableRequisites.BuildCell(tableRequisites.GetCell(2, 1))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.TOP)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .AppendText(!string.IsNullOrEmpty(client.LegalAddress) ? $"Адреса: {client.LegalAddress} " : string.Empty)
            .AddBreak()
            .AppendText(!string.IsNullOrEmpty(client.USREOU) ? $"КОД ЄДРПОУ {client.USREOU} " : string.Empty)
            .AddBreak()
            .AppendText(!string.IsNullOrEmpty(client.TIN) ? $"ІПН {client.TIN}" : string.Empty)
            .AddBreak();

        string organizationManager = !string.IsNullOrEmpty(organization.Manager) ? organization.Manager : "______________";
        string clientManager = !string.IsNullOrEmpty(client.Manager) ? client.Manager : "______________";

        tableRequisites.BuildCell(tableRequisites.GetCell(3, 0))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.BOTTOM)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .SetText($"____________________/{organizationManager}/")
            .AddBreak()
            .AppendText("    м. п./підпис");

        tableRequisites.BuildCell(tableRequisites.GetCell(3, 1))
            .SetVerticalAlignment(XWPFTableCell.XWPFVertAlign.BOTTOM)
            .CurrentParagraph
            .SetParagraphAlignment(ParagraphAlignment.LEFT)
            .SetText($"____________________/{clientManager}/")
            .AddBreak()
            .AppendText("    м. п./підпис");


        tableRequisites.SetCellsStyle("BasicCellStyle");

        document.Write(stream);

        return fileName;
    }
}