namespace Freya.Testing

open System
open System.Collections.Generic
open System.IO
open Aether
open Freya.Core
open Swensen.Unquote

#if HOPAC

open Hopac

#endif

// Defaults

/// Defaults to enable the evaluation of any function implementing Freya, given
/// a setup function of type Freya<_> to initialize the starting state of the
/// environment. A default "empty" state will be used, and the eventual state
/// will be returned for assertions to be applied as part of a test.

[<RequireQualifiedAccess>]
module Defaults =

    (* Comparers *)

    let private ordinal =
        StringComparer.Ordinal

    let private ordinalIgnoreCase =
        StringComparer.OrdinalIgnoreCase

    (* Defaults *)

    let private environment () =
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

    /// A function to give a default Freya state with some required data
    /// already present, particularly in the OWIN environment data structure.

    let state () =
        { Environment = environment ()
          Meta =
            { Memos = Map.empty } }

// Testing

// Helpers to enable the evaluation of any function implementing Freya, given
// a setup function of type Freya<_> to initialize the starting state of the
// environment. A default "empty" state will be used, and the eventual state
// will be returned for assertions to be applied as part of a test.

// Additionally a further verification function is provided which will take a
// list of assertions (of whichever test framework is relevant, simple unit
// functions) and run them against the resultant state.

// Evaluation

[<AutoOpen>]
module Evaluation =

    /// A function to evaluate a Freya function, given an initial setup
    /// function to apply modifications to the default Freya state, and a
    /// function f which may be inferred to be a Freya function (see
    /// Freya.infer). The resulting state is returned.


    let inline evaluate setup f =
        Defaults.state ()
        |> Freya.combine (Freya.infer f) setup
#if HOPAC
        |> Hopac.run
#else
        |> Async.RunSynchronously
#endif
        |> snd

// Verification

[<AutoOpen>]
module Verification =

    /// A function to verify the resulting state after the evaluation of a
    /// Freya function, given an initial setup function to apply modifications
    /// to the default Freya state, and a function f which may be inferred to
    /// be a Freya function (see Freya.infer).

    /// The final argument is a list of assertions which will be applied to the
    /// resulting state, and which may be used to signal errors to testing
    /// frameworks, etc. as part of a unit testing (or other) approach.

    let inline verify setup f assertions =
        evaluate setup f
        |> fun state ->
            List.iter (fun a -> a state) assertions

// Operators

// Helpful operators for constructing assertions for use with the verify
// functionality provided to help with testing.

module Operators =

     // Assertions

     /// An assertion (using the Unquote assertion operator) that a value
     /// obtained from the given state using an optic is equal to the value
     /// provided.

     /// This is especially useful when combined with the verify function, to
     /// construct a list of assertions in a concise and meaningful style.

    let inline (=>) o v =
        fun s -> Optic.get o s =! v
