module Freya.Testing

open System
open System.Collections.Generic
open System.IO
open Freya.Core

(* Prelude

   Helpers to enable the evaluation of any function implementing Freya, given
   a setup function of type Freya<_> to initialize the starting state of the
   environment. A default "empty" state will be used, and the eventual state
   will be returned for assertions to be applied as part of a test. *)

[<AutoOpen>]
module Prelude =

    (* Comparers *)

    let private ordinal =
        StringComparer.Ordinal

    let private ordinalIgnoreCase =
        StringComparer.OrdinalIgnoreCase

    (* Defaults *)

    let environment =
        Dictionary<string,obj> (
            Map.ofList [

                // Request

                "owin.RequestBody", Stream.Null :> obj
                "owin.RequestHeaders", Dictionary<string,string []> (ordinalIgnoreCase) :> obj
                "owin.RequestMethod", "GET" :> obj
                "owin.RequestPath", "/" :> obj
                "owin.RequestPathBase", "" :> obj
                "owin.RequestProtocol", "HTTP/1.1" :> obj
                "owin.RequestQueryString", "" :> obj
                "owin.RequestScheme", "http" :> obj

                // Response

                "owin.ResponseBody", new MemoryStream () :> obj
                "owin.ResponseHeaders", Dictionary<string,string []> (ordinalIgnoreCase) :> obj ], ordinal)

    let state =
        { Environment = environment
          Meta =
            { Memos = Map.empty } }

(* Testing

   Helpers to enable the evaluation of any function implementing Freya, given
   a setup function of type Freya<_> to initialize the starting state of the
   environment. A default "empty" state will be used, and the eventual state
   will be returned for assertions to be applied as part of a test.

   Additionally a further verification function is provided which will take a
   list of assertions (of whichever test framework is relevant, simple unit
   functions) and run them against the resultant state. *)

[<RequireQualifiedAccess>]
module Testing =

    (* Evaluation *)

    let inline evaluate setup f =
        state
        |> setup *> (Infer.freya f)
        |> Async.RunSynchronously
        |> snd

    (* Verification *)

    let inline verify setup f assertions =
        evaluate setup f
        |> fun state ->
            List.iter (fun a -> a state) assertions