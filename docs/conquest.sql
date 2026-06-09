USE [conquest]
GO
/****** Objet : UserDefinedFunction [dbo].[agent_datetime] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[agent_datetime](@date int, @time int)
RETURNS DATETIME
AS
BEGIN
 RETURN
  (
    CONVERT(DATETIME,
          CONVERT(NVARCHAR(4),@date / 10000) + N'-' + 
          CONVERT(NVARCHAR(2),(@date % 10000)/100)  + N'-' +
          CONVERT(NVARCHAR(2),@date % 100) + N' ' +        
          CONVERT(NVARCHAR(2),@time / 10000) + N':' +        
          CONVERT(NVARCHAR(2),(@time % 10000)/100) + N':' +        
          CONVERT(NVARCHAR(2),@time % 100),
    120)
  )
  END
GO
/****** Objet : Table [dbo].[DICOMStudies] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DICOMStudies](
	[StudyInsta] [varchar](64) NOT NULL,
	[StudyDate] [char](8) NULL,
	[StudyTime] [varchar](16) NULL,
	[StudyID] [varchar](16) NULL,
	[StudyDescr] [varchar](64) NULL,
	[AccessionN] [varchar](16) NULL,
	[ReferPhysi] [varchar](64) NULL,
	[PatientsAg] [varchar](16) NULL,
	[PatientsWe] [varchar](16) NULL,
	[StudyModal] [varchar](64) NULL,
	[PatientNam] [varchar](64) NULL,
	[PatientBir] [char](8) NULL,
	[PatientSex] [varchar](16) NULL,
	[PatientID] [varchar](64) NULL,
	[AccessTime] [int] NULL,
	[qTimeStamp] [int] NULL,
	[qFlags] [int] NULL,
	[qSpare] [varchar](64) NULL,
PRIMARY KEY CLUSTERED 
(
	[StudyInsta] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objet : View [dbo].[PacsStudy] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

 CREATE view [dbo].[PacsStudy] as


 SELECT [StudyInsta]
      ,convert(date, [StudyDate],103) [StudyDate]
     
      ,[StudyID]
      ,[StudyDescr]
      ,[AccessionN]
     
      ,[StudyModal] as modalite
      
      ,[PatientID] as identifiant
      ,[AccessTime]
     
  FROM  [conquest].[dbo].[DICOMStudies] 

GO
/****** Objet : View [dbo].[DICOMWorkList] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO







CREATE VIEW [dbo].[DICOMWorkList]
AS
SELECT     AccessionN, PatientID, PatientNam, PatientBir, PatientSex, MedicalAle, ContrastAl, StudyInsta, ReqPhysici, ReqProcDes, Modality, ReqContras, 
                      ScheduledA, StartDate, StartTime, PerfPhysic, SchedPSDes, SchedPSID, SchedStati, SchedPSLoc, PreMedicat, SchedPSCom, ReqProcID, ReqProcPri, 
                      AccessTime, qTimeStamp, qFlags, qSpare
FROM         Gclinique.dbo.DICOMWorklist







GO
/****** Objet : Table [dbo].[DEVICE] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DEVICE](
	[DEVICENAME] [nvarchar](50) NOT NULL,
	[DEVICEPATH] [nvarchar](300) NULL,
 CONSTRAINT [PK_DEVISE] PRIMARY KEY CLUSTERED 
(
	[DEVICENAME] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objet : Table [dbo].[DICOMImages] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DICOMImages](
	[SOPInstanc] [varchar](64) NOT NULL,
	[SOPClassUI] [varchar](64) NULL,
	[ImageNumbe] [varchar](12) NULL,
	[ImageDate] [char](8) NULL,
	[ImageTime] [varchar](16) NULL,
	[EchoNumber] [varchar](64) NULL,
	[NumberOfFr] [varchar](12) NULL,
	[AcqDate] [char](8) NULL,
	[AcqTime] [varchar](16) NULL,
	[ReceivingC] [varchar](16) NULL,
	[AcqNumber] [varchar](12) NULL,
	[SliceLocat] [varchar](16) NULL,
	[SamplesPer] [varchar](5) NULL,
	[PhotoMetri] [varchar](16) NULL,
	[QRows] [varchar](5) NULL,
	[QColumns] [varchar](5) NULL,
	[BitsStored] [varchar](5) NULL,
	[ImageType] [varchar](128) NULL,
	[ImageID] [varchar](16) NULL,
	[ImagePat] [varchar](64) NULL,
	[SeriesInst] [varchar](64) NULL,
	[AccessTime] [int] NULL,
	[qTimeStamp] [int] NULL,
	[qFlags] [int] NULL,
	[qSpare] [varchar](64) NULL,
	[ObjectFile] [varchar](255) NULL,
	[DeviceName] [varchar](32) NULL,
PRIMARY KEY CLUSTERED 
(
	[SOPInstanc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objet : Table [dbo].[DICOMPatients] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DICOMPatients](
	[PatientID] [varchar](64) NOT NULL,
	[PatientNam] [varchar](64) NULL,
	[PatientBir] [char](8) NULL,
	[PatientSex] [varchar](16) NULL,
	[AccessTime] [int] NULL,
	[qTimeStamp] [int] NULL,
	[qFlags] [int] NULL,
	[qSpare] [varchar](64) NULL,
PRIMARY KEY CLUSTERED 
(
	[PatientID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objet : Table [dbo].[DICOMSeries] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DICOMSeries](
	[SeriesInst] [varchar](64) NOT NULL,
	[SeriesNumb] [varchar](12) NULL,
	[SeriesDate] [char](8) NULL,
	[SeriesTime] [varchar](16) NULL,
	[SeriesDesc] [varchar](64) NULL,
	[Modality] [varchar](16) NULL,
	[PatientPos] [varchar](16) NULL,
	[ContrastBo] [varchar](64) NULL,
	[Manufactur] [varchar](64) NULL,
	[ModelName] [varchar](64) NULL,
	[BodyPartEx] [varchar](64) NULL,
	[ProtocolNa] [varchar](64) NULL,
	[StationNam] [varchar](16) NULL,
	[Institutio] [varchar](64) NULL,
	[FrameOfRef] [varchar](64) NULL,
	[SeriesPat] [varchar](64) NULL,
	[StudyInsta] [varchar](64) NULL,
	[AccessTime] [int] NULL,
	[qTimeStamp] [int] NULL,
	[qFlags] [int] NULL,
	[qSpare] [varchar](64) NULL,
PRIMARY KEY CLUSTERED 
(
	[SeriesInst] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objet : Table [dbo].[DICOMWorkList1] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DICOMWorkList1](
	[AccessionN] [varchar](16) NULL,
	[PatientID] [varchar](64) NULL,
	[PatientNam] [varchar](64) NULL,
	[PatientBir] [char](8) NULL,
	[PatientSex] [varchar](16) NULL,
	[MedicalAle] [varchar](64) NULL,
	[ContrastAl] [varchar](64) NULL,
	[StudyInsta] [varchar](64) NULL,
	[ReqPhysici] [varchar](64) NULL,
	[ReqProcDes] [varchar](64) NULL,
	[Modality] [varchar](16) NULL,
	[ReqContras] [varchar](64) NULL,
	[ScheduledA] [varchar](16) NULL,
	[StartDate] [char](8) NULL,
	[StartTime] [varchar](16) NULL,
	[PerfPhysic] [varchar](64) NULL,
	[SchedPSDes] [varchar](64) NULL,
	[SchedPSID] [varchar](16) NULL,
	[SchedStati] [varchar](16) NULL,
	[SchedPSLoc] [varchar](16) NULL,
	[PreMedicat] [varchar](64) NULL,
	[SchedPSCom] [varchar](64) NULL,
	[ReqProcID] [varchar](16) NULL,
	[ReqProcPri] [varchar](16) NULL,
	[AccessTime] [int] NULL,
	[qTimeStamp] [int] NULL,
	[qFlags] [int] NULL,
	[qSpare] [varchar](64) NULL
) ON [PRIMARY]
GO
/****** Objet : Table [dbo].[ETAT_TACHES] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ETAT_TACHES](
	[code] [nvarchar](2) NOT NULL,
	[designation] [nvarchar](20) NULL,
 CONSTRAINT [PK_ETAT_TACHES] PRIMARY KEY CLUSTERED 
(
	[code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objet : Table [dbo].[ImagesSize] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ImagesSize](
	[SOPInstanc] [varchar](64) NOT NULL,
	[Taille] [bigint] NULL,
	[TailleSurDisque] [bigint] NULL,
 CONSTRAINT [PK_ImagesSize] PRIMARY KEY CLUSTERED 
(
	[SOPInstanc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objet : Table [dbo].[JobGravure] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[JobGravure](
	[idTache] [nvarchar](100) NOT NULL,
	[studyUid] [nvarchar](100) NULL,
	[status] [nvarchar](100) NULL,
	[nomPatient] [nvarchar](200) NULL,
	[StudyDescription] [nvarchar](300) NULL,
	[Modality] [nvarchar](10) NULL,
	[OperatorsName] [nvarchar](100) NULL,
	[StudyDate] [nvarchar](100) NULL,
	[PatientSex] [nvarchar](10) NULL,
	[PatientID] [nvarchar](100) NULL,
	[JobDate] [datetime] NULL,
	[emplacementJob] [nvarchar](200) NULL,
	[statusGravure] [nvarchar](100) NULL,
	[nbreImage] [int] NULL,
 CONSTRAINT [PK_TacheGravure] PRIMARY KEY CLUSTERED 
(
	[idTache] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objet : Table [dbo].[MERGE_DETAILS] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MERGE_DETAILS](
	[idTache] [bigint] NOT NULL,
	[identfiant] [nvarchar](20) NOT NULL,
 CONSTRAINT [PK_MERGE_DETAILS] PRIMARY KEY CLUSTERED 
(
	[idTache] ASC,
	[identfiant] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objet : Table [dbo].[Modalite] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Modalite](
	[modalite] [nvarchar](5) NOT NULL,
	[designation] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[modalite] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objet : Table [dbo].[param] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[param](
	[code] [nvarchar](100) NOT NULL,
	[valeur] [nvarchar](50) NULL,
 CONSTRAINT [PK_param] PRIMARY KEY CLUSTERED 
(
	[code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objet : Table [dbo].[Tache] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tache](
	[idTache] [bigint] IDENTITY(1,1) NOT NULL,
	[Etat] [nvarchar](10) NULL,
	[StudyInsta] [varchar](64) NOT NULL,
	[identfiant] [nvarchar](20) NULL,
	[nom] [nvarchar](100) NULL,
	[Date_naissance] [datetime] NULL,
	[Sexe] [nvarchar](1) NULL,
	[Sysdate] [datetime] NULL,
	[StartDate] [datetime] NULL,
	[EndDate] [datetime] NULL,
	[result_msg] [nvarchar](500) NULL,
	[Type_tache] [nvarchar](10) NULL,
	[identifiant_old] [nvarchar](20) NULL,
 CONSTRAINT [PK_Tache] PRIMARY KEY CLUSTERED 
(
	[idTache] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objet : Table [dbo].[TraceDICOMImages] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TraceDICOMImages](
	[SOPInstanc] [varchar](64) NOT NULL,
	[SOPClassUI] [varchar](64) NULL,
	[ImageNumbe] [varchar](12) NULL,
	[ImageDate] [char](8) NULL,
	[ImageTime] [varchar](16) NULL,
	[EchoNumber] [varchar](64) NULL,
	[NumberOfFr] [varchar](12) NULL,
	[AcqDate] [char](8) NULL,
	[AcqTime] [varchar](16) NULL,
	[ReceivingC] [varchar](16) NULL,
	[AcqNumber] [varchar](12) NULL,
	[SliceLocat] [varchar](16) NULL,
	[SamplesPer] [varchar](5) NULL,
	[PhotoMetri] [varchar](16) NULL,
	[QRows] [varchar](5) NULL,
	[QColumns] [varchar](5) NULL,
	[BitsStored] [varchar](5) NULL,
	[ImageType] [varchar](128) NULL,
	[ImageID] [varchar](16) NULL,
	[ImagePat] [varchar](64) NULL,
	[SeriesInst] [varchar](64) NULL,
	[AccessTime] [int] NULL,
	[qTimeStamp] [int] NULL,
	[qFlags] [int] NULL,
	[qSpare] [varchar](64) NULL,
	[ObjectFile] [varchar](255) NULL,
	[DeviceName] [varchar](32) NULL,
	[UseSupp] [nvarchar](100) NULL,
	[DateSupp] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[SOPInstanc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objet : Table [dbo].[TraceDICOMSeries] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TraceDICOMSeries](
	[SeriesInst] [varchar](64) NOT NULL,
	[SeriesNumb] [varchar](12) NULL,
	[SeriesDate] [char](8) NULL,
	[SeriesTime] [varchar](16) NULL,
	[SeriesDesc] [varchar](64) NULL,
	[Modality] [varchar](16) NULL,
	[PatientPos] [varchar](16) NULL,
	[ContrastBo] [varchar](64) NULL,
	[Manufactur] [varchar](64) NULL,
	[ModelName] [varchar](64) NULL,
	[BodyPartEx] [varchar](64) NULL,
	[ProtocolNa] [varchar](64) NULL,
	[StationNam] [varchar](16) NULL,
	[Institutio] [varchar](64) NULL,
	[FrameOfRef] [varchar](64) NULL,
	[SeriesPat] [varchar](64) NULL,
	[StudyInsta] [varchar](64) NULL,
	[AccessTime] [int] NULL,
	[qTimeStamp] [int] NULL,
	[qFlags] [int] NULL,
	[qSpare] [varchar](64) NULL,
	[UseSupp] [nvarchar](100) NULL,
	[DateSupp] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[SeriesInst] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objet : Table [dbo].[TraceDICOMStudies] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TraceDICOMStudies](
	[StudyInsta] [varchar](64) NOT NULL,
	[StudyDate] [char](8) NULL,
	[StudyTime] [varchar](16) NULL,
	[StudyID] [varchar](16) NULL,
	[StudyDescr] [varchar](64) NULL,
	[AccessionN] [varchar](16) NULL,
	[ReferPhysi] [varchar](64) NULL,
	[PatientsAg] [varchar](16) NULL,
	[PatientsWe] [varchar](16) NULL,
	[StudyModal] [varchar](64) NULL,
	[PatientNam] [varchar](64) NULL,
	[PatientBir] [char](8) NULL,
	[PatientSex] [varchar](16) NULL,
	[PatientID] [varchar](64) NULL,
	[AccessTime] [int] NULL,
	[qTimeStamp] [int] NULL,
	[qFlags] [int] NULL,
	[qSpare] [varchar](64) NULL,
	[UseSupp] [nvarchar](100) NULL,
	[DateSupp] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[StudyInsta] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objet : Table [dbo].[TraceModification] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TraceModification](
	[id] [numeric](18, 0) IDENTITY(1,1) NOT NULL,
	[dicomtag] [nvarchar](50) NULL,
	[oldValue] [nvarchar](400) NULL,
	[newValue] [nvarchar](400) NULL,
	[sysdate] [datetime] NULL,
	[idtache] [bigint] NULL,
	[StudyInsta] [varchar](64) NOT NULL,
	[SeriesInst] [varchar](64) NOT NULL,
	[SOPInstanc] [varchar](64) NOT NULL,
 CONSTRAINT [PK_TraceModification] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objet : Table [dbo].[TYPE_TACHE] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TYPE_TACHE](
	[code] [nvarchar](10) NOT NULL,
	[Designation] [nvarchar](50) NULL,
 CONSTRAINT [PK_TYPE_TACHE] PRIMARY KEY CLUSTERED 
(
	[code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objet : Table [dbo].[UIDMODS] Date de script : 02/06/2026 11:23:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UIDMODS](
	[MODTime] [int] NULL,
	[OldUID] [varchar](64) NULL,
	[MODType] [varchar](32) NULL,
	[NewUID] [varchar](64) NULL,
	[Stage] [varchar](32) NULL,
	[Annotation] [varchar](64) NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[MERGE_DETAILS]  WITH CHECK ADD  CONSTRAINT [FK_MERGE_DETAILS_Tache] FOREIGN KEY([idTache])
REFERENCES [dbo].[Tache] ([idTache])
GO
ALTER TABLE [dbo].[MERGE_DETAILS] CHECK CONSTRAINT [FK_MERGE_DETAILS_Tache]
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "DICOMWorklist (Gclinique.dbo)"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 114
               Right = 229
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
      Begin ColumnWidths = 9
         Width = 284
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 11
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'DICOMWorkList'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'DICOMWorkList'
GO
