namespace adr

module String =
    let splitByChar (seps : char array) (s : string) = s.Split(seps)
    let replace (oldValue : string) (newValue : string) (s : string) =
        s.Replace(oldValue, newValue) 
    let lower (s : string) = s.ToLowerInvariant()
