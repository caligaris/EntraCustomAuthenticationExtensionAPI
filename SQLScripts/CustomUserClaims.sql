/****** Object:  Table [dbo].[CustomAppClaims]    Script Date: 3/21/2024 6:38:44 PM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CustomUserClaims]') AND type in (N'U'))
DROP TABLE [dbo].[CustomUserClaims]
GO

/****** Object:  Table [dbo].[CustomAppClaims]    Script Date: 3/21/2024 6:38:44 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[CustomUserClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserPrincipalName] [nchar](50) NOT NULL,
	[ClaimName] [nchar](50) NULL,
	[ClaimValue] [nchar](50) NULL,
 CONSTRAINT [PK_CustomUserClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


