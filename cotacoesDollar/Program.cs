using System;
using HtmlAgilityPack;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Collections;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using System.Linq.Expressions;
using System.Threading;

namespace cotacoesDollar
{
    class Program
    {
        static void Main(string[] args)
        {
            /*==== > O site fornece somente o valor atual de cotação e não o histórico para captura via htmlAgility ou regex, portanto será realizado o seguinte procedimento:
            1 - A Cotação do Dolar será recuperada do site valor.globo.com sempre que a cotação mudar, ou não existir registro anterior feito pelo programa.
            2 - O valor é armazenado usando uma classe como tipo, e o registro é armazenado em um bd caso nao exista ou seja diferente (valor da cotação) este bd será um arquivo txt (formato json)
            3 - Será Printado na Console os valores solicitados nos requisitos, a tela atualizará a cada 60 segundos e verifica se os dados da nova requisição diferen dos já armazenados, se não, não armazena
            */

            //validacao do arquivo de base de dados em json
            string txtFileLocation = Directory.GetCurrentDirectory() + "\\database.txt";

            while (true) 
            {
                DateTime timer = DateTime.Now;
                TimeSpan totaltime = new TimeSpan(); 

                Console.Write("Ola, voce tem na tela os valores sumarizados do dia (minimo, maximo e media)\n" +
                              "1 - A cada 1 minuto a tela se atualiza, e ocorre uma nova consulta no site \n" +
                              "2 - Os valores mostrados aqui sao um cumulativo dos dados coletados pelo programa (enquanto ele estava aberto) e que foram armazenados no banco de dados (txt em formato json)\n" +
                              "3 - So serao adicionados novos registros no banco de dados caso o valor da cotacao em questao seja diferente do ultimo armazenado no banco de dados \n" +
                              "4 - na descrição dos registos existe a informacao se o valor é atual da ultima consulta, ou é um valor do banco, pois se manteve sem alteração desde a ultima consulta, assim como a data da consulta. \n" +
                              "-Fonte: https://valor.globo.com/valor-data/ + \n" +
                              "-Local do Banco de Dados Json (txt): " + txtFileLocation + "\n\n");


                List<DollarData> allDollarCotationsFromSite = getAtualValuesFromSite(); //todas cotacoes vindas do site ja transformadas em objeto
                List<string> allDollarCotationsJson = new List<string>(); // todas as cotacoes que serao transformadas em json caso precisem ir para o banco de dados (txt)
                List<DollarData> allDollarCotationsFromDB = new List<DollarData>(); //todas as recuperadas do banco de dados

                if (!File.Exists(txtFileLocation)) //se nao existe banco de dados, cria e armazena os valores de consulta atual
                {
                    foreach (var dollarCotationSite in allDollarCotationsFromSite)
                    {
                        printDollarDataObject(dollarCotationSite, "site");
                        allDollarCotationsJson.Add(convertObjectToJson(dollarCotationSite));
                    }
                }
                else //se existe, vai identificar o nome das cotações vindas do site, e via procurá-las no banco de dados, 
                    //caso haja alteração vai adicionar no banco, se o valor se mantiver o mesmo, irá mostrar o registro resgatado de cada cotação
                {
                    Console.Write("\n \n ======> ULTIMOS REGISTROS:" + "\n");
                    allDollarCotationsFromDB = getOnlyLastDataFromDB(txtFileLocation);
                    bool cotationprocessed = false;

                    foreach (var dollarCotationSite in allDollarCotationsFromSite)
                    {
                        foreach (var dollarCotationDB in allDollarCotationsFromDB)
                        {
                            if (dollarCotationSite.TypeName.Equals(dollarCotationDB.TypeName))
                            {
                                if (!dollarCotationSite.AtualValue.Equals(dollarCotationDB.AtualValue))
                                {
                                    printDollarDataObject(dollarCotationSite, "site");
                                    allDollarCotationsJson.Add(convertObjectToJson(dollarCotationSite)); //adiciona na lista, pra colocar no banco de dados
                                }
                                else
                                {
                                    printDollarDataObject(dollarCotationDB, "db");
                                }
                                cotationprocessed = true;
                            }
                        }

                        if (cotationprocessed == false)
                        {
                            allDollarCotationsJson.Add(convertObjectToJson(dollarCotationSite));
                        }
                    }
                }
                File.AppendAllLines(txtFileLocation, allDollarCotationsJson); //imprime tudo que tem no json agendado pra ir pro banco
                allDollarCotationsJson = new List<string>(); //reseta a lista de coisas que tem que ir pro banco
                List<string> valuesMinMaxAvg = getMinMaxAvgValueCotation(txtFileLocation); //obtem os calores min max avg de cada cotação encontrada na webrequest
                summOfTheDay(valuesMinMaxAvg); //imprime o resumo na tela
                totaltime = DateTime.Now.Subtract(timer); //para de contar o tempo, imprime o tempo decorrido deste ciclo
                Console.WriteLine("\n \n Tempo Total de Execucao (hh:mm:sss): " + totaltime.TotalHours.ToString("00") + ":" + totaltime.Minutes.ToString("00") + ":" + totaltime.TotalSeconds.ToString("000"));
                Thread.Sleep(60000); //aguarda 60 segundos para faze ruma nova consulta e recomeçar o ciclo.
                Console.Clear();
            }

        }           


        private static void summOfTheDay(List<string> valuesMinMaxAvg)
        {
            Console.Write("\n \n ======> RESUMO DO DIA (" + DateTime.Now.ToString("dd-MM") + "): " + "\n");

            foreach (var estatisticas in valuesMinMaxAvg)
            {
                Console.Write("\nNome da Cotacão: " + estatisticas.Split(";")[0] +
                              " / Min.: " + estatisticas.Split(";")[1] +
                              " / Max.: " + estatisticas.Split(";")[2] +
                              " / Med.: " + estatisticas.Split(";")[3]);
            }

            Console.Write("\n\n");
        }

        private static void printDollarDataObject(DollarData obj,string fonte)
        {

            if (!fonte.Equals("db"))
            {
                Console.Write("\nTipo de Cotacao: " + obj.TypeName + "\n" +
                              "Valor Atual: " + obj.AtualValue + "\n" +
                              "Alta/baixa %: " + obj.AtualPercent + "\n" +
                              "Data do Registro: " + obj.Date + "\n" +
                              "Fonte do Dado: Consulta Atual" + "\n" +
                              "----------------------------------------------------------------------");
            }
            else 
            {
                Console.Write("\nTipo de Cotacao: " + obj.TypeName + "\n" +
                                  "Valor Atual: " + obj.AtualValue + "\n" +
                                  "Alta/baixa %: " + obj.AtualPercent + "\n" +
                                  "Data do Registro: " + obj.Date + "\n" +
                                  "Fonte do Dado: Registro do Banco, pois o site ainda nao teve alteração de preco na cotacao (verificado as " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ")" +  "\n" +
                                  "----------------------------------------------------------------------");

            }
        }

        private static List<DollarData> getAtualValuesFromSite()
        {
            const string url = "https://valor.globo.com/valor-data/";
            var browser = new HtmlWeb();
            HtmlDocument webPage = new HtmlDocument();

            try
            {
                webPage = browser.Load(url);
            }
            catch (WebException error)
            {
                List<string> errorToPrint = new List<string>() { ("Erro ao acessar a url [" + url + "]." + "Erro: " + error.Message) };
                File.AppendAllLines((Directory.GetCurrentDirectory() + "\\" + DateTime.Now.ToString("yyyyMMdd") + ".log"), errorToPrint);
                Console.WriteLine("Erro ao acessar o site para coleta das informacoes: " + errorToPrint[0]);
                Console.WriteLine("Saindo da aplicacao em 15 segundos...");
                Thread.Sleep(15000);
                Environment.Exit(0);
            }
            

            List<DollarData> allDollarCotations = new List<DollarData>();

            //Obtendo os nodes div referente às moedas
            List<HtmlNode> divsCurrencies = webPage.DocumentNode.Descendants("div").Where(selectedNodes => selectedNodes.GetAttributeValue("class", "").Equals("cell large-auto data-cotacao__ticker")).ToList();

            //percorrendo a div internamente, para coletar seu respectivo valor e relação anterior (porcentagem subida/descida)

            foreach (var atualDiv in divsCurrencies) 
            {
                if (atualDiv.InnerText.Contains("Dólar")) 
                {
                    DollarData dollarUniqueCotation = new DollarData
                    {
                        AtualValue = (atualDiv.Descendants("div").Where(selectedNodes => selectedNodes.GetAttributeValue("class", "").Contains("cotacao__ticker_quote")).First().InnerText).Replace(",", "."),
                        AtualPercent = atualDiv.Descendants("div").Where(selectedNodes => selectedNodes.GetAttributeValue("class", "").Contains("cotacao__ticker_percentage")).First().InnerText,
                        Date = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"),
                        TypeName = atualDiv.Descendants("div").Where(selectedNodes => selectedNodes.GetAttributeValue("class", "").Contains("data-cotacao__ticker_name")).First().InnerText
                };
                    allDollarCotations.Add(dollarUniqueCotation);
                }
            }
            return allDollarCotations;
        }

        private static string convertObjectToJson(DollarData objDollarData)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(DollarData));
            MemoryStream memStream = new MemoryStream();
            ser.WriteObject(memStream, objDollarData);
            string jsonOutput = Encoding.UTF8.GetString(memStream.ToArray());
            memStream.Close();

            return jsonOutput;
        }

        private static DollarData convertjsonToObject(string jsonDollarData)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(DollarData));
            MemoryStream memStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonDollarData));
            DollarData dollaDataObj = (DollarData)serializer.ReadObject(memStream);
            return dollaDataObj;

        }

        private static List<DollarData> getOnlyLastDataFromDB(string txtFileLocation)
        {
            List<DollarData> allDataFromDBObj = GetAllDataFromDB(txtFileLocation);
            List<string> cotationsNamesToSearch = new List<string>();
            List<DollarData> selectedCotationsToReturn = new List<DollarData>();

            foreach (var cotationName in allDataFromDBObj)
            {
                if (cotationsNamesToSearch.Contains(cotationName.TypeName))
                {
                    continue;
                }
                else {
                    cotationsNamesToSearch.Add(cotationName.TypeName);
                }
            
            }


            foreach (var cotationName in cotationsNamesToSearch)
            {
                for (int i = 0; i < allDataFromDBObj.Count(); i++) 
                {
                    if (cotationName.Equals(allDataFromDBObj[allDataFromDBObj.Count() - 1 - i].TypeName))
                    {
                        selectedCotationsToReturn.Add(allDataFromDBObj[allDataFromDBObj.Count() - 1 - i]);
                        break;
                    }
                }
            }
            return selectedCotationsToReturn;
        }

        private static List<DollarData> GetAllDataFromDB(string txtFileLocation)
        {
            List<string> allDataFromDBJson = (File.ReadLines(txtFileLocation).ToList());
            List<DollarData> allDataFromDBObj = new List<DollarData>();

            foreach (var datafromDB in allDataFromDBJson)
            {
                allDataFromDBObj.Add(convertjsonToObject(datafromDB));
            }
            return allDataFromDBObj;
        }
        private static List<string> getMinMaxAvgValueCotation(string txtFileLocation)
        {
            List<DollarData> allDataFromDBObj = GetAllDataFromDB(txtFileLocation);
            List<double> minMaxAvgOfCurrency = new List<double> { 0, 0, 0}; //min, max, avg
            List<string> dataToReturn = new List<string>();
            List<string> cotationsNamesToSearch = new List<string>();

            foreach (var cotationName in allDataFromDBObj)
            {
                if (cotationsNamesToSearch.Contains(cotationName.TypeName))
                {
                    continue;
                }
                else
                {
                    cotationsNamesToSearch.Add(cotationName.TypeName);
                }

            }

            int count = 0;
            foreach (var name in cotationsNamesToSearch)
            {
                count = 0;
                minMaxAvgOfCurrency = new List<double> { 0, 0, 0 };
                foreach (var valueCotation in allDataFromDBObj)
                {
                    if (valueCotation.TypeName.Equals(name) && (DateTime.ParseExact(valueCotation.Date, "dd-MM-yyyy HH:mm:ss", null).Day) == (DateTime.Now.Day))
                    {
                        if (count == 0)
                        {
                            minMaxAvgOfCurrency[0] = Math.Round(Double.Parse(valueCotation.AtualValue), 2);
                            minMaxAvgOfCurrency[1] = Math.Round(Double.Parse(valueCotation.AtualValue), 2);
                            minMaxAvgOfCurrency[2] = Math.Round(Double.Parse(valueCotation.AtualValue), 2);
                        }
                        else
                        {
                            if (Math.Round(Double.Parse(valueCotation.AtualValue), 2) < minMaxAvgOfCurrency[0]) //min
                            {
                                minMaxAvgOfCurrency[0] = Math.Round(Double.Parse(valueCotation.AtualValue), 2);
                            }

                            if (Math.Round(Double.Parse(valueCotation.AtualValue), 2) > minMaxAvgOfCurrency[1]) //max
                            {
                                minMaxAvgOfCurrency[1] = Math.Round(Double.Parse(valueCotation.AtualValue), 2);
                            }
                            minMaxAvgOfCurrency[2] += Math.Round(Double.Parse(valueCotation.AtualValue), 2); //avg
                        }
                        count++;
                    }
                }
                minMaxAvgOfCurrency[2] = Math.Round(minMaxAvgOfCurrency[2] / count, 2); //divisao final avg
                dataToReturn.Add(name + ";" + minMaxAvgOfCurrency[0] + ";" + minMaxAvgOfCurrency[1] + ";" + minMaxAvgOfCurrency[2]);
            }
            return dataToReturn;
        }
    }
}
