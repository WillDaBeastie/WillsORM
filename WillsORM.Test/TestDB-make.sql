USE [master]
GO
/****** Object:  Database [TestDB]    Script Date: 07/12/2017 16:26:28 ******/
CREATE DATABASE [TestDB] ON  PRIMARY 
( NAME = N'TestDB', FILENAME = N'c:\Program Files\Microsoft SQL Server\MSSQL10_50.SQLEXPRESS\MSSQL\DATA\TestDB.mdf' , SIZE = 3072KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'TestDB_log', FILENAME = N'c:\Program Files\Microsoft SQL Server\MSSQL10_50.SQLEXPRESS\MSSQL\DATA\TestDB_log.ldf' , SIZE = 1024KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO
ALTER DATABASE [TestDB] SET COMPATIBILITY_LEVEL = 100
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [TestDB].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [TestDB] SET ANSI_NULL_DEFAULT OFF
GO
ALTER DATABASE [TestDB] SET ANSI_NULLS OFF
GO
ALTER DATABASE [TestDB] SET ANSI_PADDING OFF
GO
ALTER DATABASE [TestDB] SET ANSI_WARNINGS OFF
GO
ALTER DATABASE [TestDB] SET ARITHABORT OFF
GO
ALTER DATABASE [TestDB] SET AUTO_CLOSE OFF
GO
ALTER DATABASE [TestDB] SET AUTO_CREATE_STATISTICS ON
GO
ALTER DATABASE [TestDB] SET AUTO_SHRINK OFF
GO
ALTER DATABASE [TestDB] SET AUTO_UPDATE_STATISTICS ON
GO
ALTER DATABASE [TestDB] SET CURSOR_CLOSE_ON_COMMIT OFF
GO
ALTER DATABASE [TestDB] SET CURSOR_DEFAULT  GLOBAL
GO
ALTER DATABASE [TestDB] SET CONCAT_NULL_YIELDS_NULL OFF
GO
ALTER DATABASE [TestDB] SET NUMERIC_ROUNDABORT OFF
GO
ALTER DATABASE [TestDB] SET QUOTED_IDENTIFIER OFF
GO
ALTER DATABASE [TestDB] SET RECURSIVE_TRIGGERS OFF
GO
ALTER DATABASE [TestDB] SET  DISABLE_BROKER
GO
ALTER DATABASE [TestDB] SET AUTO_UPDATE_STATISTICS_ASYNC OFF
GO
ALTER DATABASE [TestDB] SET DATE_CORRELATION_OPTIMIZATION OFF
GO
ALTER DATABASE [TestDB] SET TRUSTWORTHY OFF
GO
ALTER DATABASE [TestDB] SET ALLOW_SNAPSHOT_ISOLATION OFF
GO
ALTER DATABASE [TestDB] SET PARAMETERIZATION SIMPLE
GO
ALTER DATABASE [TestDB] SET READ_COMMITTED_SNAPSHOT OFF
GO
ALTER DATABASE [TestDB] SET HONOR_BROKER_PRIORITY OFF
GO
ALTER DATABASE [TestDB] SET  READ_WRITE
GO
ALTER DATABASE [TestDB] SET RECOVERY SIMPLE
GO
ALTER DATABASE [TestDB] SET  MULTI_USER
GO
ALTER DATABASE [TestDB] SET PAGE_VERIFY CHECKSUM
GO
ALTER DATABASE [TestDB] SET DB_CHAINING OFF
GO
USE [TestDB]
GO
/****** Object:  User [TestDB]    Script Date: 07/12/2017 16:26:28 ******/
CREATE USER [TestDB] FOR LOGIN [testDB] WITH DEFAULT_SCHEMA=[dbo]
GO
/****** Object:  Table [dbo].[ThingTwo_ThingThree]    Script Date: 07/12/2017 16:26:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ThingTwo_ThingThree](
	[ThingTwoID] [int] NOT NULL,
	[ThingThreeID] [int] NOT NULL,
	[Order] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ThingTwo]    Script Date: 07/12/2017 16:26:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ThingTwo](
	[ThingTwoID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](150) NULL,
	[Description] [varchar](max) NULL,
	[ThingOneID] [int] NULL,
 CONSTRAINT [PK_ThingTwo] PRIMARY KEY CLUSTERED 
(
	[ThingTwoID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[ThingThree]    Script Date: 07/12/2017 16:26:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ThingThree](
	[ThingThreeID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](150) NULL,
	[Description] [varchar](150) NULL,
	[SomethingElse] [varchar](150) NULL,
 CONSTRAINT [PK_ThingThree] PRIMARY KEY CLUSTERED 
(
	[ThingThreeID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[ThingOne]    Script Date: 07/12/2017 16:26:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[ThingOne](
	[ThingOneID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](150) NULL,
	[Score] [int] NULL,
 CONSTRAINT [PK_ThingOne] PRIMARY KEY CLUSTERED 
(
	[ThingOneID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
