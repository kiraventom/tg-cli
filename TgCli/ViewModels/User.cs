﻿namespace TgCli.ViewModels;

public class User : IRenderUser
{
    public long Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string Username { get; }
    public bool IsOnline { get; set; }

    public User(long id, string firstName, string lastName, string username)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Username = username;
    }
}