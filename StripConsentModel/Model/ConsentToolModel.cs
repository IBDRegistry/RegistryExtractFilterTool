﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;
using System.Text;

namespace StripV3Consent.Model
{
    public class ConsentToolModel
    {
        public readonly ObservableCollection<ImportFile> InputFiles = new ObservableCollection<ImportFile>();        

        private void InputFiles_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Patients.Clear();
            IEnumerable<RecordSet> NewPatients = SplitInputFilesIntoRecordSets(InputFiles);
            Patients.AddRange(NewPatients);

            OutputFiles.Clear();
            IEnumerable<OutputFile> NewOutputFiles = SplitBackUpIntoFiles(Patients);
            foreach (OutputFile File in NewOutputFiles)
			{
                OutputFiles.Add(File);
			}
        }

        public readonly ObservableRangeCollection<RecordSet> Patients = new ObservableRangeCollection<RecordSet>();

        public readonly ObservableCollection<OutputFile> OutputFiles = new ObservableCollection<OutputFile>();

        public ConsentToolModel()
        {
            InputFiles.CollectionChanged += InputFiles_CollectionChanged;
        }

        /// <summary>
        /// Splits ImportFile objects into Records, and then groups them by identifier to make RecordSets
        /// </summary>
        /// <param name="Files"></param>
        /// <returns></returns>
        private IEnumerable<RecordSet> SplitInputFilesIntoRecordSets(IEnumerable<ImportFile> Files)
        {
            List<Record> AllRecords = Files
                                            .Select(File => File.SplitInto2DArray().Content         //Transform DataFile into string[][] resulting in IEnumerable<string[][]>
                                               .Select(Row => new Record(Row, File)))                             //Transform each string[] into Record resulting in IEnumerable<IEnumerable<Record>>
                                                .SelectMany<IEnumerable<Record>, Record>(x => x)                //Flatten into IEnumerable<Record>
                                                .ToList<Record>();                                            //Cast to List<Record>
                                                                                                              //Task<string[][]>[] AllTasks = Files.Select(async File => (await File.SplitInto2DArray()).Content).ToArray();

            //Group records by identifiers (NHS Number & Date of Birth)
            IEnumerable<RecordSet> GroupedRecords = AllRecords.GroupBy
                                                                        <Record, string, RecordSet>(//Take in Record, group by String, Output RecordSet
                                                                        r => r.CompositeIdentifier,
                                                                        (NHSNumber, RecordsIEnumerable) => new RecordSet()
                                                                        {
                                                                            Records = RecordsIEnumerable.ToList<Record>()
                                                                        }
                                                                        );

            return GroupedRecords;
        }

        private IEnumerable<IEnumerable<Record>> FlattenAndGroupRecordsByOriginalFiles(IEnumerable<RecordSet> RecordSets)
        {
            IEnumerable<Record> RecordsFlattened = RecordSets.Select(rs => rs.Records).SelectMany<IEnumerable<Record>, Record>(x => x);

            IEnumerable<Record> OutputRecords = RecordsFlattened.Where(r => r.OriginalFile.SpecificationFile.IsRegistryFile == true);

            IEnumerable<IEnumerable<Record>> RecordsGroupedByOriginalFiles = OutputRecords.GroupBy
                                                                                <Record, DataFile, IEnumerable<Record>>(
                                                                                r => r.OriginalFile,    //Group by original file
                                                                                (OriginalFile, RecordsIEnumerable) => RecordsIEnumerable    //Output groupings as IEnumerable<Record>
                                                                                );
            return RecordsGroupedByOriginalFiles;
        }

        private IEnumerable<OutputFile> SplitBackUpIntoFiles(IEnumerable<RecordSet> RecordSets)
        {
            IEnumerable<IEnumerable<Record>> AllConsentedRecordsGroupedByFile = FlattenAndGroupRecordsByOriginalFiles(RecordSets.Where(RS => RS.IsConsentValid == true));

            //Have to do ToList in order to use Find() later on which isn't present in IEnumerable<T>
            List<IEnumerable<Record>> AllRecordsGroupedByFile = FlattenAndGroupRecordsByOriginalFiles(RecordSets).ToList<IEnumerable<Record>>();


            IEnumerable<OutputFile> Files = AllConsentedRecordsGroupedByFile.Select(RecordsInOutputFile => new OutputFile(RecordsInOutputFile.First().OriginalFile) //Match each set of records to a new OutputFile object
                                                                {
                                                                    OutputRecords = RecordsInOutputFile,    //Make the OutputRecords the current set of (consented) records
                                                                    AllRecordsOriginallyInFile = AllRecordsGroupedByFile.Find(RecordIEnumerable => RecordIEnumerable.First().OriginalFile == RecordsInOutputFile.First().OriginalFile)
                                                                    //Find the full set (consented and non-consented) of records by looking through AllRecordsGroupedByFile for one with the same OriginalFile attribute
                                                                }
                                                            );
            return Files;
        }

    }
}