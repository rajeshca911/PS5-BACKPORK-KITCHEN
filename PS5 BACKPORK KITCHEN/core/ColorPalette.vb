Imports System.Drawing

''' <summary>
''' Standardized color palette for consistent UI theming across the application.
''' Provides primary, status, and feature button colors following modern design principles.
''' </summary>
Public Module ColorPalette

    ' ===========================
    ' PRIMARY COLORS
    ' ===========================

    ''' <summary>Primary brand color - Modern blue</summary>
    Public ReadOnly Property Primary As Color
        Get
            Return Color.FromArgb(0, 120, 215)
        End Get
    End Property

    ''' <summary>Primary dark variant for hover states</summary>
    Public ReadOnly Property PrimaryDark As Color
        Get
            Return Color.FromArgb(0, 95, 184)
        End Get
    End Property

    ''' <summary>Primary light variant for backgrounds</summary>
    Public ReadOnly Property PrimaryLight As Color
        Get
            Return Color.FromArgb(204, 228, 247)
        End Get
    End Property

    ' ===========================
    ' STATUS COLORS
    ' ===========================

    ''' <summary>Success state - Green</summary>
    Public ReadOnly Property Success As Color
        Get
            Return Color.FromArgb(16, 124, 16)
        End Get
    End Property

    ''' <summary>Warning state - Amber</summary>
    Public ReadOnly Property Warning As Color
        Get
            Return Color.FromArgb(255, 185, 0)
        End Get
    End Property

    ''' <summary>Error/Danger state - Red</summary>
    Public ReadOnly Property [Error] As Color
        Get
            Return Color.FromArgb(232, 17, 35)
        End Get
    End Property

    ''' <summary>Info state - Light blue</summary>
    Public ReadOnly Property Info As Color
        Get
            Return Color.FromArgb(0, 120, 212)
        End Get
    End Property

    ' ===========================
    ' FEATURE BUTTON COLORS
    ' ===========================

    ''' <summary>Feature button - Light sky blue</summary>
    Public ReadOnly Property FeatureBlue As Color
        Get
            Return Color.FromArgb(135, 206, 250)
        End Get
    End Property

    ''' <summary>Feature button - Light green</summary>
    Public ReadOnly Property FeatureGreen As Color
        Get
            Return Color.FromArgb(144, 238, 144)
        End Get
    End Property

    ''' <summary>Feature button - Medium purple</summary>
    Public ReadOnly Property FeaturePurple As Color
        Get
            Return Color.FromArgb(186, 85, 211)
        End Get
    End Property

    ''' <summary>Feature button - Light coral</summary>
    Public ReadOnly Property FeatureCoral As Color
        Get
            Return Color.FromArgb(240, 128, 128)
        End Get
    End Property

    ''' <summary>Feature button - Gold</summary>
    Public ReadOnly Property FeatureGold As Color
        Get
            Return Color.FromArgb(255, 215, 0)
        End Get
    End Property

    ''' <summary>Feature button - Cyan</summary>
    Public ReadOnly Property FeatureCyan As Color
        Get
            Return Color.FromArgb(64, 224, 208)
        End Get
    End Property

    ''' <summary>Feature button - Teal</summary>
    Public ReadOnly Property FeatureTeal As Color
        Get
            Return Color.FromArgb(0, 150, 136)
        End Get
    End Property

    ' ===========================
    ' DARK THEME COLORS
    ' ===========================

    ''' <summary>Dark theme background</summary>
    Public ReadOnly Property DarkBackground As Color
        Get
            Return Color.FromArgb(32, 32, 32)
        End Get
    End Property

    ''' <summary>Dark theme surface (panels, cards)</summary>
    Public ReadOnly Property DarkSurface As Color
        Get
            Return Color.FromArgb(45, 45, 45)
        End Get
    End Property

    ''' <summary>Dark theme text</summary>
    Public ReadOnly Property DarkText As Color
        Get
            Return Color.FromArgb(255, 255, 255)
        End Get
    End Property

    ''' <summary>Dark theme secondary text</summary>
    Public ReadOnly Property DarkTextSecondary As Color
        Get
            Return Color.FromArgb(180, 180, 180)
        End Get
    End Property

    ' ===========================
    ' LIGHT THEME COLORS
    ' ===========================

    ''' <summary>Light theme background</summary>
    Public ReadOnly Property LightBackground As Color
        Get
            Return Color.FromArgb(255, 255, 255)
        End Get
    End Property

    ''' <summary>Light theme surface (panels, cards)</summary>
    Public ReadOnly Property LightSurface As Color
        Get
            Return Color.FromArgb(245, 245, 245)
        End Get
    End Property

    ''' <summary>Light theme text</summary>
    Public ReadOnly Property LightText As Color
        Get
            Return Color.FromArgb(0, 0, 0)
        End Get
    End Property

    ''' <summary>Light theme secondary text</summary>
    Public ReadOnly Property LightTextSecondary As Color
        Get
            Return Color.FromArgb(100, 100, 100)
        End Get
    End Property

    ' ===========================
    ' UTILITY COLORS
    ' ===========================

    ''' <summary>Border color for controls</summary>
    Public ReadOnly Property Border As Color
        Get
            Return Color.FromArgb(200, 200, 200)
        End Get
    End Property

    ''' <summary>Disabled state color</summary>
    Public ReadOnly Property Disabled As Color
        Get
            Return Color.FromArgb(150, 150, 150)
        End Get
    End Property

    ''' <summary>Transparent overlay</summary>
    Public ReadOnly Property Overlay As Color
        Get
            Return Color.FromArgb(128, 0, 0, 0)
        End Get
    End Property

End Module