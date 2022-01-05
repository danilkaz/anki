﻿using System;
using System.Threading;
using App;
using App.Converters;
using App.SerializedClasses;
using App.UIClasses;
using Domain;
using Domain.LearnMethods;
using Infrastructure;
using Ninject;
using Ninject.Extensions.Conventions;
using Npgsql;
using UI;
using UI.Commands;
using UI.Config;
using UI.Dialogs;

namespace AnkiBot
{
    public static class Program
    {
        private const string PostgresConnectionString = "Host=localhost;Username=postgres;Password=postgres;" +
                                                        "Database=postgres;Port=5433"; //TODO: убрать это куда-нибудь (например в env)

        private const string SqliteConnectionString = "Data source=db.db";

        private static readonly string Database =
            Environment.GetEnvironmentVariable("BOT_DATABASE", EnvironmentVariableTarget.User);

        public static void Main()
        {
            using var container = CreateContainer();

            container.Get<IDatabase<DbCard>>().CreateTable();
            container.Get<IDatabase<DbDeck>>().CreateTable();

            foreach (var bot in container.GetAll<IBot>())
                new Thread(bot.Start).Start();
            Console.ReadLine();
        }

        private static StandardKernel CreateContainer()
        {
            var container = new StandardKernel();

            container.Bind<VkConfig>().ToSelf().InSingletonScope();
            container.Bind<TelegramConfig>().ToSelf().InSingletonScope();

            if (Database == "Sqlite")
            {
                container.Bind<IDatabase<DbCard>>().To<SqLiteDatabase<DbCard>>().InSingletonScope()
                    .WithConstructorArgument(SqliteConnectionString);
                container.Bind<IDatabase<DbDeck>>().To<SqLiteDatabase<DbDeck>>().InSingletonScope()
                    .WithConstructorArgument(SqliteConnectionString);
            }
            else
            {
                container.Bind<NpgsqlConnection>().ToSelf().WithConstructorArgument(PostgresConnectionString);
                container.Bind<IDatabase<DbCard>>().To<PostgresDatabase<DbCard>>().InSingletonScope();
                container.Bind<IDatabase<DbDeck>>().To<PostgresDatabase<DbDeck>>().InSingletonScope();
            }

            container.Bind<IRepository<DbCard>>().To<CardRepository>().InSingletonScope();
            container.Bind<IRepository<DbDeck>>().To<DeckRepository>().InSingletonScope();

            container.Bind<IConverter<DbCard, UICard, Card>>().To<CardConverter>().InSingletonScope();
            container.Bind<IConverter<DbDeck, UIDeck, Deck>>().To<DeckConverter>().InSingletonScope();

            container.Bind<CardApi>().ToSelf();
            container.Bind<DeckApi>().ToSelf();

            container.Bind(c =>
                c.FromAssemblyContaining<Command>().SelectAllClasses().InheritedFrom<Command>().BindAllBaseClasses());
            container.Bind(c =>
                c.FromAssemblyContaining<IDialog>().SelectAllClasses().InheritedFrom<IDialog>().BindAllInterfaces());
            container.Bind(c =>
                c.FromAssemblyContaining<ILearnMethod>().SelectAllClasses().InheritedFrom<ILearnMethod>()
                    .BindAllInterfaces());

            container.Bind<BotHandler>().ToSelf().InSingletonScope();
            container.Bind<IBot>().To<TelegramBot>().InSingletonScope();
            container.Bind<IBot>().To<VkBot>().InSingletonScope();

            return container;
        }
    }
}

//TODO: сделать запись состояний в базу данных (как такое организовать)
//TODO: пофиксить карточки (чтобы плохо изученные карточки появлялись вновь) (это todo не от Сорокина :))