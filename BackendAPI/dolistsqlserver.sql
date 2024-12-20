-- Cambiar a "Master" y eliminar DB si existe
Use Master;
Go

Drop Database If Exists DoListDB;
Go

-- Crear y usar base de datos creada
Create Database DoListDB;
Go

Use DoListDB;
Go

-- Tablas

-- Tabla "Roles"
Create Table Roles (
    roleId Int Primary Key Identity,
    name Varchar(50) Not Null
)
Go

-- Tabla "Users"
Create Table Users (
    userId Int Primary Key Identity,
    name Nvarchar(255) Not Null,
    userName Nvarchar(255) Not Null Unique,
    dni Int Not Null Unique,
    email Nvarchar(255) Not Null Unique,
    password Nvarchar(255) Not Null,
    roleId Int Not Null Default 2 References Roles(roleId) On Delete No Action,
    createdAt Datetime Default GetDate(),
    updatedAt Datetime Default GetDate()
)
Go

-- Tabla "Projects"
Create Table Projects (
    projectId Int Primary Key Identity,
    userId Int Not Null References Users(userId) On Delete No Action,
    name Nvarchar(255) Not Null,
    color Char(7) Null,
    isFavorite Bit Default 0,
    createdAt Datetime Default GetDate(),
    updatedAt Datetime Default GetDate()
)
Go

-- Tabla "Tasks"
Create Table Tasks (
    taskId Int Primary Key Identity,
    projectId Int Not Null References Projects(projectId) On Delete Cascade,
    name Nvarchar(255) Not Null,
    description Text Null,
    dueDate Date Null,
    priority TinyInt Default 1,
    isCompleted Bit Default 0,
    createdAt Datetime Default GetDate(),
    updatedAt Datetime Default GetDate()
)
Go

-- Tabla "Comments"
Create Table Comments (
    commentId Int Primary Key Identity,
    taskId Int Not Null References Tasks(taskId) On Delete Cascade,
    userId Int Not Null References Users(userId) On Delete No Action,
    content Text Not Null,
    createdAt Datetime Default GetDate(),
    updatedAt Datetime Default GetDate(),
)
Go

-- Tabla "ProjectInvitations"
Create Table ProjectInvitations (
    projectId Int Not Null References Projects(projectId) On Delete Cascade,
    userId Int Not Null References Users(userId) On Delete Cascade,
    createdAt Datetime Default GetDate(),
    updatedAt Datetime Default GetDate(),
    Primary Key (projectId, userId)
)
Go

-- Tabla "TaskAssignments"
Create Table TaskAssignments(
    taskId Int Not Null References Tasks(taskId) On Delete Cascade,
    userId Int Not Null References Users(userId) On Delete Cascade,
    createdAt Datetime Default GetDate(),
    updatedAt Datetime Default GetDate(),
    Primary Key (taskid, userId)
)
Go

-- Roles
Select * From Roles Go

-- Users
Select * From Users Go

-- Projects
Select * From Projects Go

-- Tasks
Select * From Tasks Go

-- Comments
Select * From Comments Go

-- ProjectInvitations
Select * From ProjectInvitations Go

-- TaskAssignments
Select * From TaskAssignments Go
